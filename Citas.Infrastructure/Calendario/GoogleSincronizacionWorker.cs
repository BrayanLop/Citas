using Citas.Application.Calendario;
using Citas.Domain.Calendario;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Citas.Infrastructure.Calendario;

/// <summary>
/// Proceso en segundo plano que concilia el Google Calendar de cada empleado conectado. Cada
/// pasada es barata (compara con la base de datos y solo llama a Google si hay diferencias o si
/// toca la lectura periódica), así que corre seguido para que una cita nueva aparezca en
/// segundos. Un fallo con un empleado no detiene a los demás.
/// </summary>
public sealed class GoogleSincronizacionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _fabrica;
    private readonly IEjecutorPorTenant _ejecutor;
    private readonly OpcionesGoogle _opciones;
    private readonly ILogger<GoogleSincronizacionWorker> _log;

    public GoogleSincronizacionWorker(
        IServiceScopeFactory fabrica,
        IEjecutorPorTenant ejecutor,
        OpcionesGoogle opciones,
        ILogger<GoogleSincronizacionWorker> log)
    {
        _fabrica = fabrica;
        _ejecutor = ejecutor;
        _opciones = opciones;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Sincronización con Google Calendar activa (cada {Segundos} s).", _opciones.FrecuenciaSalidaSegundos);
        using var reloj = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, _opciones.FrecuenciaSalidaSegundos)));

        do
        {
            try
            {
                await PasadaAsync(stoppingToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _log.LogError(e, "Falló una pasada de sincronización con Google Calendar.");
            }
        }
        while (await SiguienteAsync(reloj, stoppingToken));
    }

    internal async Task PasadaAsync(CancellationToken ct)
    {
        IReadOnlyList<CuentaPendiente> cuentas;
        using (var alcance = _fabrica.CreateScope())
        {
            cuentas = await alcance.ServiceProvider.GetRequiredService<ICalendarioRepositorio>().ListarCuentasActivasAsync(ct);
        }

        foreach (var cuenta in cuentas)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await _ejecutor.EjecutarAsync<SincronizadorCalendario>(
                    cuenta.Tenant, (s, token) => s.SincronizarEmpleadoAsync(cuenta.Tenant, cuenta.IdEmpleado, token), ct);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _log.LogError(e, "No se pudo sincronizar al empleado {Empleado} de {Tenant}.", cuenta.IdEmpleado, cuenta.Tenant);
            }
        }
    }

    private static async Task<bool> SiguienteAsync(PeriodicTimer reloj, CancellationToken ct)
    {
        try { return await reloj.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
