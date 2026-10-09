using Citas.Models;

namespace Citas.Domain.Agendamiento;

/// <summary>
/// Repositorio de la agenda de los empleados: horario semanal (AgendaEmpleado) y
/// bloqueos/ausencias (BloqueoAgenda). Todo dentro del tenant actual.
/// </summary>
public interface IAgendaEmpleadoRepository
{
    /// <summary>Horario semanal del empleado (sin franjas si nunca se definió).</summary>
    Task<AgendaEmpleado> ObtenerHorarioAsync(int idEmpleado, CancellationToken cancellationToken = default);

    /// <summary>Reemplaza todas las franjas del empleado por las de <paramref name="agenda"/>.</summary>
    Task ReemplazarHorarioAsync(AgendaEmpleado agenda, CancellationToken cancellationToken = default);

    /// <summary>Bloqueos del empleado que se cruzan con [desdeUtc, hastaUtc), por inicio.</summary>
    Task<IReadOnlyList<BloqueoAgenda>> ListarBloqueosAsync(
        int idEmpleado,
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default);

    Task<BloqueoAgenda?> ObtenerBloqueoAsync(int idEmpleado, int idBloqueo, CancellationToken cancellationToken = default);

    Task AgregarBloqueoAsync(BloqueoAgenda bloqueo, CancellationToken cancellationToken = default);

    void EliminarBloqueo(BloqueoAgenda bloqueo);

    /// <summary>
    /// Serializa, hasta el fin de la transacción en curso, las escrituras que tocan la
    /// agenda de un empleado (crear/reprogramar citas, crear bloqueos), para que dos
    /// peticiones simultáneas no validen el mismo hueco a la vez. Requiere transacción.
    /// </summary>
    Task BloquearAgendaAsync(int idEmpleado, CancellationToken cancellationToken = default);
}
