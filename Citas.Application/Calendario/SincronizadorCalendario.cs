using System.Security.Cryptography;
using System.Text;
using Citas.Application.Agendamiento;
using Citas.Application.Common;
using Citas.Domain.Agendamiento;
using Citas.Domain.Calendario;
using Citas.Models;
using Microsoft.Extensions.Logging;

namespace Citas.Application.Calendario;

/// <summary>
/// Mantiene al día el Google Calendar de UN empleado, en las dos direcciones, por conciliación:
/// compara lo que debería haber con lo que ya está publicado y corrige la diferencia. Por eso es
/// idempotente y se recupera solo de fallos (si algo no salió, la siguiente pasada lo repite):
///  - Salida (Citas → Google): cada cita vigente tiene un evento; si cambia se actualiza, si se
///    cancela o se borra se elimina. El id del evento es determinista, así reintentar no duplica.
///  - Entrada (Google → Citas): los eventos ocupados del calendario del empleado se vuelven
///    bloqueos de su agenda (sin copiar títulos: son privados). Los eventos que creó Citas se ignoran.
/// La base de datos de Citas sigue siendo la fuente de verdad de las citas.
/// </summary>
public class SincronizadorCalendario
{
    private const string PrefijoEvento = "citas";
    private const string MotivoBloqueo = "Ocupado en Google Calendar";

    private readonly ICalendarioRepositorio _repo;
    private readonly ICitaRepository _citas;
    private readonly IAgendaEmpleadoRepository _agendas;
    private readonly IUnitOfWork _uow;
    private readonly ICalendarioExterno _google;
    private readonly ICifradorSecretos _cifrador;
    private readonly FirmasGoogle _firmas;
    private readonly RelojAgenda _reloj;
    private readonly OpcionesGoogle _opciones;
    private readonly ILogger<SincronizadorCalendario> _log;

    public SincronizadorCalendario(
        ICalendarioRepositorio repo,
        ICitaRepository citas,
        IAgendaEmpleadoRepository agendas,
        IUnitOfWork uow,
        ICalendarioExterno google,
        ICifradorSecretos cifrador,
        FirmasGoogle firmas,
        RelojAgenda reloj,
        OpcionesGoogle opciones,
        ILogger<SincronizadorCalendario> log)
    {
        _repo = repo;
        _citas = citas;
        _agendas = agendas;
        _uow = uow;
        _google = google;
        _cifrador = cifrador;
        _firmas = firmas;
        _reloj = reloj;
        _opciones = opciones;
        _log = log;
    }

    /// <summary>Id del evento de una cita: determinista y válido para Google (minúsculas y dígitos).</summary>
    public static string IdEvento(string tenant, int idCita)
        => PrefijoEvento + Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes($"{tenant}:{idCita}"))).ToLowerInvariant();

    /// <summary>Huella del contenido publicado: si cambia, hay que actualizar el evento.</summary>
    public static string Huella(Cita c)
    {
        var contenido = string.Join('|',
            c.Inicio.ToString("O"), c.Fin.ToString("O"), c.Estado, c.Cliente?.NombreUsuario,
            string.Join(',', c.Detalles.Select(d => d.NombreServicio)), c.Observaciones);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(contenido)))[..24];
    }

    public async Task SincronizarEmpleadoAsync(string tenant, int idEmpleado, CancellationToken ct = default)
    {
        var cuenta = await _repo.ObtenerCuentaAsync(idEmpleado, ct);
        var ahora = _reloj.AhoraUtc;
        if (cuenta is null || !cuenta.PuedeIntentar(ahora))
            return;

        var refresh = _cifrador.Descifrar(cuenta.RefreshTokenCifrado);
        try
        {
            await SalidaAsync(tenant, cuenta, refresh, ahora, ct);
            if (cuenta.Toca(ahora, TimeSpan.FromSeconds(_opciones.IntervaloEntranteSegundos)))
            {
                await EntradaAsync(cuenta, refresh, ahora, ct);
                await AsegurarCanalAsync(cuenta, refresh, ahora, ct);
            }
            cuenta.RegistrarExito();
        }
        catch (CredencialesRevocadasException e)
        {
            _log.LogWarning("Google revocó el acceso del empleado {Empleado} de {Tenant}: {Motivo}", idEmpleado, tenant, e.Message);
            cuenta.Revocar(e.Message);
            _uow.DescartarCambios();
            // Los bloqueos que vinieron de Google ya no son confiables: se quitan hasta reconectar.
            await _repo.EliminarBloqueosDeGoogleAsync(idEmpleado, null, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _log.LogWarning(e, "No se pudo sincronizar Google Calendar del empleado {Empleado} de {Tenant}", idEmpleado, tenant);
            _uow.DescartarCambios();
            cuenta.RegistrarFallo(e.Message, ahora);
        }

        _repo.ActualizarCuenta(cuenta);
        await _uow.GuardarCambiosAsync(ct);
    }

    /// <summary>Un aviso (webhook) de Google: sincronizar en la próxima pasada sin esperar el intervalo.</summary>
    public async Task MarcarCambioRemotoAsync(int idEmpleado, CancellationToken ct = default)
    {
        var cuenta = await _repo.ObtenerCuentaAsync(idEmpleado, ct);
        if (cuenta is null || !cuenta.EstaActiva) return;
        cuenta.MarcarCambioRemoto();
        _repo.ActualizarCuenta(cuenta);
        await _uow.GuardarCambiosAsync(ct);
    }

    // ---- Citas -> Google ---------------------------------------------------------------

    private async Task SalidaAsync(string tenant, CuentaGoogle cuenta, string refresh, DateTime ahora, CancellationToken ct)
    {
        var vinculos = (await _repo.ListarVinculosCitaAsync(cuenta.IdEmpleado, ct)).ToDictionary(v => v.IdCita);
        var citas = await _citas.ListarParaCalendarioAsync(cuenta.IdEmpleado, ahora, vinculos.Keys.ToList(), ct);
        var porId = citas.ToDictionary(c => c.IdCita);

        foreach (var cita in citas)
        {
            ct.ThrowIfCancellationRequested();
            var vigente = cita.IdEmpleado == cuenta.IdEmpleado && cita.Estado != EstadoCita.Cancelada;
            var existe = vinculos.TryGetValue(cita.IdCita, out var vinculo);

            if (vigente && !existe && cita.Fin >= ahora)
            {
                // Una cita nueva (o una anterior a conectar la cuenta que aún no termina).
                var id = IdEvento(tenant, cita.IdCita);
                await _google.PublicarEventoAsync(refresh, cuenta.CalendarioId, id, ComoEvento(cita), ct);
                await _repo.AgregarVinculoCitaAsync(cuenta.IdEmpleado, new VinculoCita(cita.IdCita, id, Huella(cita)), ct);
                await _uow.GuardarCambiosAsync(ct);
            }
            else if (vigente && existe && vinculo!.Huella != Huella(cita))
            {
                await _google.PublicarEventoAsync(refresh, cuenta.CalendarioId, vinculo.GoogleEventId, ComoEvento(cita), ct);
                await _repo.ActualizarHuellaAsync(cita.IdCita, Huella(cita), ct);
                await _uow.GuardarCambiosAsync(ct);
            }
            else if (!vigente && existe)
            {
                // Cancelada, o reprogramada con otro empleado: sale de este calendario.
                await RetirarAsync(cuenta, refresh, vinculo!, ct);
            }
        }

        // Citas borradas: quedó el vínculo pero la cita ya no existe.
        foreach (var huerfano in vinculos.Values.Where(v => !porId.ContainsKey(v.IdCita)))
            await RetirarAsync(cuenta, refresh, huerfano, ct);
    }

    private async Task RetirarAsync(CuentaGoogle cuenta, string refresh, VinculoCita vinculo, CancellationToken ct)
    {
        await _google.EliminarEventoAsync(refresh, cuenta.CalendarioId, vinculo.GoogleEventId, ct);
        await _repo.EliminarVinculoCitaAsync(vinculo.IdCita, ct);
        await _uow.GuardarCambiosAsync(ct);
    }

    private static EventoCalendario ComoEvento(Cita c)
    {
        var servicios = string.Join(", ", c.Detalles.Select(d => d.NombreServicio).Where(n => !string.IsNullOrWhiteSpace(n)));
        var titulo = string.IsNullOrWhiteSpace(servicios) ? "Cita" : servicios;
        if (!string.IsNullOrWhiteSpace(c.Cliente?.NombreUsuario)) titulo += $" · {c.Cliente.NombreUsuario}";
        if (c.Estado == EstadoCita.Pendiente) titulo = $"[Pendiente] {titulo}";

        var descripcion = string.IsNullOrWhiteSpace(c.Observaciones) ? "Cita agendada con Natural." : $"{c.Observaciones}\n\nCita agendada con Natural.";
        return new EventoCalendario(titulo, descripcion, c.Inicio, c.Fin, c.IdCita);
    }

    // ---- Google -> Citas ---------------------------------------------------------------

    private async Task EntradaAsync(CuentaGoogle cuenta, string refresh, DateTime ahora, CancellationToken ct)
    {
        var desde = ahora.AddDays(-1);
        var hasta = ahora.AddDays(_opciones.DiasHaciaAdelante);

        CambiosCalendario cambios;
        try
        {
            cambios = await _google.ListarCambiosAsync(refresh, cuenta.CalendarioId, cuenta.SyncToken, desde, hasta, ct);
        }
        catch (SyncTokenVencidoException)
        {
            cuenta.InvalidarSyncToken();
            cambios = await _google.ListarCambiosAsync(refresh, cuenta.CalendarioId, null, desde, hasta, ct);
        }

        // Carga completa: se reconstruyen todos los bloqueos de Google desde cero.
        if (cambios.CargaCompleta)
            await _repo.EliminarBloqueosDeGoogleAsync(cuenta.IdEmpleado, null, ct);

        var conocidos = cambios.CargaCompleta
            ? new HashSet<string>()
            : (await _repo.ListarVinculosBloqueoAsync(cuenta.IdEmpleado, ct)).Select(v => v.GoogleEventId).ToHashSet();

        foreach (var evento in cambios.Eventos)
        {
            ct.ThrowIfCancellationRequested();
            if (evento.CreadoPorCitas || evento.Id.StartsWith(PrefijoEvento, StringComparison.Ordinal))
                continue;   // lo publicó Citas: no se vuelve a importar como bloqueo

            if (conocidos.Contains(evento.Id))
                await _repo.EliminarBloqueosDeGoogleAsync(cuenta.IdEmpleado, [evento.Id], ct);

            if (evento.Cancelado || !evento.OcupaTiempo)
                continue;

            var (inicio, fin) = Intervalo(evento);
            if (fin <= inicio || fin <= ahora)
                continue;   // vacío o ya pasó

            var bloqueo = BloqueoAgenda.Crear(cuenta.IdEmpleado, inicio, fin, MotivoBloqueo);
            await _agendas.AgregarBloqueoAsync(bloqueo, ct);
            await _uow.GuardarCambiosAsync(ct);
            await _repo.AgregarVinculoBloqueoAsync(cuenta.IdEmpleado, bloqueo.IdBloqueo, evento.Id, ct);
            await _uow.GuardarCambiosAsync(ct);
        }

        cuenta.RegistrarSincronizacionEntrante(cambios.SyncToken, ahora);
    }

    private (DateTime Inicio, DateTime Fin) Intervalo(EventoOcupado e)
    {
        if (e.DiaCompleto)
        {
            // Día(s) completos: de la medianoche local del primer día a la del día siguiente al último.
            return (_reloj.AUtc(DateOnly.FromDateTime(e.Inicio), TimeOnly.MinValue),
                    _reloj.AUtc(DateOnly.FromDateTime(e.Fin), TimeOnly.MinValue));
        }
        return (DateTime.SpecifyKind(e.Inicio, DateTimeKind.Utc), DateTime.SpecifyKind(e.Fin, DateTimeKind.Utc));
    }

    // ---- Avisos push (opcional) --------------------------------------------------------

    private async Task AsegurarCanalAsync(CuentaGoogle cuenta, string refresh, DateTime ahora, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_opciones.UrlWebhook)) return;
        if (cuenta.CanalId is not null && cuenta.CanalExpiraUtc > ahora.AddHours(24)) return;

        try
        {
            var canalId = Guid.NewGuid().ToString("N");
            var expira = await _google.RegistrarCanalAsync(
                refresh, cuenta.CalendarioId, canalId, _opciones.UrlWebhook, _firmas.TokenCanal(canalId), ct);
            cuenta.RegistrarCanal(canalId, expira);
        }
        catch (CredencialesRevocadasException)
        {
            throw;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // Sin avisos push se sigue sincronizando por intervalo: no es motivo para fallar.
            _log.LogWarning(e, "No se pudo registrar el canal de notificaciones del empleado {Empleado}", cuenta.IdEmpleado);
        }
    }
}
