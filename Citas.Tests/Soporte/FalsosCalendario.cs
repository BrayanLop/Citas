using Citas.Application.Calendario;
using Citas.Domain.Calendario;

namespace Citas.Tests.Soporte;

/// <summary>"Cifrado" de mentira, reconocible a simple vista: prefijo + texto invertido.</summary>
public sealed class CifradorFalso : ICifradorSecretos
{
    public string Cifrar(string textoPlano) => "enc:" + new string(textoPlano.Reverse().ToArray());
    public string Descifrar(string textoCifrado) => new string(textoCifrado["enc:".Length..].Reverse().ToArray());
}

/// <summary>Google Calendar en memoria: guarda lo que se publica y deja programar respuestas y fallos.</summary>
public sealed class CalendarioExternoFalso : ICalendarioExterno
{
    public Dictionary<string, EventoCalendario> Publicados { get; } = [];
    public List<string> Eliminados { get; } = [];
    public List<string> TokensUsados { get; } = [];
    public int LlamadasPublicar { get; private set; }
    public int LlamadasListar { get; private set; }

    /// <summary>Si no es null, toda llamada lanza esto (simula Google caído o permiso revocado).</summary>
    public Exception? Fallo { get; set; }

    /// <summary>Qué devuelve ListarCambios: recibe el syncToken con el que se pidió.</summary>
    public Func<string?, CambiosCalendario> Cambios { get; set; } = _ => new CambiosCalendario([], "sync-1", true);

    public List<string?> SyncTokensPedidos { get; } = [];
    public int CanalesRegistrados { get; private set; }

    public Task PublicarEventoAsync(string refreshToken, string calendarioId, string eventoId, EventoCalendario evento, CancellationToken ct = default)
    {
        Validar(refreshToken);
        LlamadasPublicar++;
        Publicados[eventoId] = evento;
        return Task.CompletedTask;
    }

    public Task EliminarEventoAsync(string refreshToken, string calendarioId, string eventoId, CancellationToken ct = default)
    {
        Validar(refreshToken);
        Publicados.Remove(eventoId);
        Eliminados.Add(eventoId);
        return Task.CompletedTask;
    }

    public Task<CambiosCalendario> ListarCambiosAsync(
        string refreshToken, string calendarioId, string? syncToken, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
    {
        Validar(refreshToken);
        LlamadasListar++;
        SyncTokensPedidos.Add(syncToken);
        return Task.FromResult(Cambios(syncToken));
    }

    public Task<DateTime> RegistrarCanalAsync(string refreshToken, string calendarioId, string canalId, string urlWebhook, string token, CancellationToken ct = default)
    {
        Validar(refreshToken);
        CanalesRegistrados++;
        return Task.FromResult(new DateTime(2026, 12, 2, 0, 0, 0, DateTimeKind.Utc));
    }

    private void Validar(string refreshToken)
    {
        TokensUsados.Add(refreshToken);
        if (Fallo is not null) throw Fallo;
    }
}

public sealed class GoogleOAuthFalso : IGoogleOAuth
{
    public AutorizacionGoogle Autorizacion { get; set; } = new("refresh-123", "barbero@gmail.com");
    public List<string> Revocados { get; } = [];
    public string? CodigoRecibido { get; private set; }
    public bool FallarAlRevocar { get; set; }

    public string UrlAutorizacion(string state) => "https://accounts.example/auth?state=" + Uri.EscapeDataString(state);

    public Task<AutorizacionGoogle> CanjearCodigoAsync(string codigo, CancellationToken ct = default)
    {
        CodigoRecibido = codigo;
        return Task.FromResult(Autorizacion);
    }

    public Task RevocarAsync(string refreshToken, CancellationToken ct = default)
    {
        if (FallarAlRevocar) throw new HttpRequestException("sin red");
        Revocados.Add(refreshToken);
        return Task.CompletedTask;
    }
}

/// <summary>Persistencia del calendario en memoria; los bloqueos viven en el repositorio de agenda.</summary>
public sealed class CalendarioRepositorioMemoria(RepositorioAgendaMemoria agenda) : ICalendarioRepositorio
{
    public List<CuentaGoogle> Cuentas { get; } = [];
    public List<(int IdEmpleado, VinculoCita Vinculo)> VinculosCita { get; } = [];
    public List<(int IdEmpleado, VinculoBloqueo Vinculo)> VinculosBloqueo { get; } = [];
    private int _idCuenta = 1;

    public Task<CuentaGoogle?> ObtenerCuentaAsync(int idEmpleado, CancellationToken ct = default)
        => Task.FromResult(Cuentas.FirstOrDefault(c => c.IdEmpleado == idEmpleado));

    public Task AgregarCuentaAsync(CuentaGoogle cuenta, CancellationToken ct = default)
    {
        // Misma cuenta con id asignado, como haría la base de datos.
        Cuentas.Add(CuentaGoogle.Rehidratar(_idCuenta++, cuenta.IdEmpleado, cuenta.Email, cuenta.RefreshTokenCifrado,
            cuenta.Estado, cuenta.CalendarioId, cuenta.SyncToken, cuenta.UltimaSincronizacionUtc, cuenta.CambioRemotoPendiente,
            cuenta.Intentos, cuenta.ProximoIntentoUtc, cuenta.UltimoError, cuenta.CanalId, cuenta.CanalExpiraUtc, cuenta.ConectadaUtc));
        return Task.CompletedTask;
    }

    // La cuenta que devuelve ObtenerCuenta es la misma instancia: los cambios ya están guardados.
    public void ActualizarCuenta(CuentaGoogle cuenta) { }

    public Task EliminarCuentaAsync(int idEmpleado, CancellationToken ct = default)
    {
        Cuentas.RemoveAll(c => c.IdEmpleado == idEmpleado);
        VinculosCita.RemoveAll(v => v.IdEmpleado == idEmpleado);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CuentaPendiente>> ListarCuentasActivasAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CuentaPendiente>>(Cuentas.Where(c => c.EstaActiva).Select(c => new CuentaPendiente("T1", c.IdEmpleado)).ToList());

    public Task<CuentaPendiente?> BuscarPorCanalAsync(string canalId, CancellationToken ct = default)
        => Task.FromResult(Cuentas.Where(c => c.CanalId == canalId).Select(c => new CuentaPendiente("T1", c.IdEmpleado)).FirstOrDefault());

    public Task<IReadOnlyList<VinculoCita>> ListarVinculosCitaAsync(int idEmpleado, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<VinculoCita>>(VinculosCita.Where(v => v.IdEmpleado == idEmpleado).Select(v => v.Vinculo).ToList());

    public Task AgregarVinculoCitaAsync(int idEmpleado, VinculoCita vinculo, CancellationToken ct = default)
    {
        VinculosCita.Add((idEmpleado, vinculo));
        return Task.CompletedTask;
    }

    public Task ActualizarHuellaAsync(int idCita, string huella, CancellationToken ct = default)
    {
        var i = VinculosCita.FindIndex(v => v.Vinculo.IdCita == idCita);
        if (i >= 0) VinculosCita[i] = (VinculosCita[i].IdEmpleado, VinculosCita[i].Vinculo with { Huella = huella });
        return Task.CompletedTask;
    }

    public Task EliminarVinculoCitaAsync(int idCita, CancellationToken ct = default)
    {
        VinculosCita.RemoveAll(v => v.Vinculo.IdCita == idCita);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VinculoBloqueo>> ListarVinculosBloqueoAsync(int idEmpleado, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<VinculoBloqueo>>(VinculosBloqueo.Where(v => v.IdEmpleado == idEmpleado).Select(v => v.Vinculo).ToList());

    public Task AgregarVinculoBloqueoAsync(int idEmpleado, int idBloqueo, string googleEventId, CancellationToken ct = default)
    {
        VinculosBloqueo.Add((idEmpleado, new VinculoBloqueo(idBloqueo, googleEventId)));
        return Task.CompletedTask;
    }

    public Task EliminarBloqueosDeGoogleAsync(int idEmpleado, IReadOnlyCollection<string>? soloEventos, CancellationToken ct = default)
    {
        var quitar = VinculosBloqueo
            .Where(v => v.IdEmpleado == idEmpleado && (soloEventos is null || soloEventos.Contains(v.Vinculo.GoogleEventId)))
            .ToList();
        foreach (var v in quitar)
        {
            agenda.Bloqueos.RemoveAll(b => b.IdBloqueo == v.Vinculo.IdBloqueo);
            VinculosBloqueo.Remove(v);
        }
        return Task.CompletedTask;
    }
}
