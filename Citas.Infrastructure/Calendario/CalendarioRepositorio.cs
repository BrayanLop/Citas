using Citas.Data;
using Citas.Domain.Calendario;
using Citas.Infrastructure.Persistencia;
using Citas.Infrastructure.Persistencia.Modelos;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Calendario;

// Persistencia de la integración con Google Calendar. El aislamiento por tenant lo aplican los
// filtros globales del contexto; solo ListarCuentasActivasAsync y BuscarPorCanalAsync lo cruzan
// (a propósito: los usan el proceso en segundo plano y el webhook, que no traen token).
public class CalendarioRepositorio : ICalendarioRepositorio
{
    private readonly CitasContext _db;

    public CalendarioRepositorio(CitasContext db) => _db = db;

    // ---- Cuenta ----

    public async Task<CuentaGoogle?> ObtenerCuentaAsync(int idEmpleado, CancellationToken ct = default)
    {
        var fila = await _db.CuentasGoogle.AsNoTracking().FirstOrDefaultAsync(c => c.IdEmpleado == idEmpleado, ct);
        return fila is null ? null : ADominio(fila);
    }

    public async Task AgregarCuentaAsync(CuentaGoogle cuenta, CancellationToken ct = default)
        => await _db.CuentasGoogle.AddAsync(ADb(cuenta), ct);

    public void ActualizarCuenta(CuentaGoogle cuenta) => _db.CuentasGoogle.Update(ADb(cuenta));

    public async Task EliminarCuentaAsync(int idEmpleado, CancellationToken ct = default)
    {
        await _db.GoogleEventosCita.Where(v => v.IdEmpleado == idEmpleado).ExecuteDeleteAsync(ct);
        await _db.CuentasGoogle.Where(c => c.IdEmpleado == idEmpleado).ExecuteDeleteAsync(ct);
    }

    public async Task<IReadOnlyList<CuentaPendiente>> ListarCuentasActivasAsync(CancellationToken ct = default)
        => await _db.CuentasGoogle.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.Estado == nameof(EstadoCuentaGoogle.Activa))
            .Select(c => new CuentaPendiente(c.TenantId, c.IdEmpleado))
            .ToListAsync(ct);

    public async Task<CuentaPendiente?> BuscarPorCanalAsync(string canalId, CancellationToken ct = default)
        => await _db.CuentasGoogle.IgnoreQueryFilters().AsNoTracking()
            .Where(c => c.CanalId == canalId)
            .Select(c => new CuentaPendiente(c.TenantId, c.IdEmpleado))
            .FirstOrDefaultAsync(ct);

    // ---- Citas publicadas ----

    public async Task<IReadOnlyList<VinculoCita>> ListarVinculosCitaAsync(int idEmpleado, CancellationToken ct = default)
        => await _db.GoogleEventosCita.AsNoTracking()
            .Where(v => v.IdEmpleado == idEmpleado)
            .Select(v => new VinculoCita(v.IdCita, v.GoogleEventId, v.Huella))
            .ToListAsync(ct);

    public async Task AgregarVinculoCitaAsync(int idEmpleado, VinculoCita vinculo, CancellationToken ct = default)
        => await _db.GoogleEventosCita.AddAsync(new GoogleEventoCitaDbModel
        {
            IdCita = vinculo.IdCita,
            IdEmpleado = idEmpleado,
            GoogleEventId = vinculo.GoogleEventId,
            Huella = vinculo.Huella,
        }, ct);

    public async Task ActualizarHuellaAsync(int idCita, string huella, CancellationToken ct = default)
        => await _db.GoogleEventosCita.Where(v => v.IdCita == idCita)
            .ExecuteUpdateAsync(s => s.SetProperty(v => v.Huella, huella), ct);

    public async Task EliminarVinculoCitaAsync(int idCita, CancellationToken ct = default)
        => await _db.GoogleEventosCita.Where(v => v.IdCita == idCita).ExecuteDeleteAsync(ct);

    // ---- Bloqueos que vienen de Google ----

    public async Task<IReadOnlyList<VinculoBloqueo>> ListarVinculosBloqueoAsync(int idEmpleado, CancellationToken ct = default)
        => await _db.GoogleBloqueos.AsNoTracking()
            .Where(v => v.IdEmpleado == idEmpleado)
            .Select(v => new VinculoBloqueo(v.IdBloqueo, v.GoogleEventId))
            .ToListAsync(ct);

    public async Task AgregarVinculoBloqueoAsync(int idEmpleado, int idBloqueo, string googleEventId, CancellationToken ct = default)
        => await _db.GoogleBloqueos.AddAsync(new GoogleBloqueoDbModel
        {
            IdBloqueo = idBloqueo,
            IdEmpleado = idEmpleado,
            GoogleEventId = googleEventId,
        }, ct);

    public async Task EliminarBloqueosDeGoogleAsync(int idEmpleado, IReadOnlyCollection<string>? soloEventos, CancellationToken ct = default)
    {
        var vinculos = _db.GoogleBloqueos.Where(v => v.IdEmpleado == idEmpleado);
        if (soloEventos is not null)
        {
            var eventos = soloEventos.ToList();
            vinculos = vinculos.Where(v => eventos.Contains(v.GoogleEventId));
        }

        var idsBloqueo = await vinculos.Select(v => v.IdBloqueo).ToListAsync(ct);
        // Borrar el bloqueo se lleva su vínculo (cascada).
        await _db.BloqueosAgenda.Where(b => idsBloqueo.Contains(b.IdBloqueo)).ExecuteDeleteAsync(ct);
    }

    // ---- Mapeo ----

    private static CuentaGoogle ADominio(CuentaGoogleDbModel f)
        => CuentaGoogle.Rehidratar(
            f.IdCuenta, f.IdEmpleado, f.Email, f.RefreshTokenCifrado,
            Enum.Parse<EstadoCuentaGoogle>(f.Estado), f.CalendarioId, f.SyncToken, f.UltimaSincronizacionUtc,
            f.CambioRemotoPendiente, f.Intentos, f.ProximoIntentoUtc, f.UltimoError, f.CanalId, f.CanalExpiraUtc,
            f.ConectadaUtc);

    private static CuentaGoogleDbModel ADb(CuentaGoogle c)
        => new()
        {
            IdCuenta = c.IdCuenta,
            IdEmpleado = c.IdEmpleado,
            Email = c.Email,
            RefreshTokenCifrado = c.RefreshTokenCifrado,
            Estado = c.Estado.ToString(),
            CalendarioId = c.CalendarioId,
            SyncToken = c.SyncToken,
            UltimaSincronizacionUtc = c.UltimaSincronizacionUtc,
            CambioRemotoPendiente = c.CambioRemotoPendiente,
            Intentos = c.Intentos,
            ProximoIntentoUtc = c.ProximoIntentoUtc,
            UltimoError = c.UltimoError,
            CanalId = c.CanalId,
            CanalExpiraUtc = c.CanalExpiraUtc,
            ConectadaUtc = c.ConectadaUtc,
        };
}
