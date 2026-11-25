namespace Citas.Domain.Calendario;

/// <summary>Cita de Citas que ya tiene su evento en el Google Calendar del empleado.</summary>
public sealed record VinculoCita(int IdCita, string GoogleEventId, string Huella);

/// <summary>Bloqueo de Citas que nació de un evento ocupado del Google Calendar del empleado.</summary>
public sealed record VinculoBloqueo(int IdBloqueo, string GoogleEventId);

/// <summary>Una cuenta activa de cualquier empresa (para el proceso en segundo plano).</summary>
public sealed record CuentaPendiente(string Tenant, int IdEmpleado);

/// <summary>
/// Persistencia de la integración con el calendario externo. Todo es del tenant actual salvo
/// <see cref="ListarCuentasActivasAsync"/> y <see cref="BuscarPorCanalAsync"/>, que cruzan empresas
/// a propósito (los usa el proceso en segundo plano y el webhook, que no tienen token).
/// </summary>
public interface ICalendarioRepositorio
{
    Task<CuentaGoogle?> ObtenerCuentaAsync(int idEmpleado, CancellationToken ct = default);
    Task AgregarCuentaAsync(CuentaGoogle cuenta, CancellationToken ct = default);
    void ActualizarCuenta(CuentaGoogle cuenta);
    Task EliminarCuentaAsync(int idEmpleado, CancellationToken ct = default);

    Task<IReadOnlyList<CuentaPendiente>> ListarCuentasActivasAsync(CancellationToken ct = default);
    Task<CuentaPendiente?> BuscarPorCanalAsync(string canalId, CancellationToken ct = default);

    Task<IReadOnlyList<VinculoCita>> ListarVinculosCitaAsync(int idEmpleado, CancellationToken ct = default);
    Task AgregarVinculoCitaAsync(int idEmpleado, VinculoCita vinculo, CancellationToken ct = default);
    Task ActualizarHuellaAsync(int idCita, string huella, CancellationToken ct = default);
    Task EliminarVinculoCitaAsync(int idCita, CancellationToken ct = default);

    Task<IReadOnlyList<VinculoBloqueo>> ListarVinculosBloqueoAsync(int idEmpleado, CancellationToken ct = default);
    Task AgregarVinculoBloqueoAsync(int idEmpleado, int idBloqueo, string googleEventId, CancellationToken ct = default);

    /// <summary>Quita los vínculos y los bloqueos creados desde Google (todos, o solo los de esos eventos).</summary>
    Task EliminarBloqueosDeGoogleAsync(int idEmpleado, IReadOnlyCollection<string>? soloEventos, CancellationToken ct = default);
}
