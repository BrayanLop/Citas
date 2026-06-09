using Citas.Models;

namespace Citas.Domain.Agendamiento;

/// <summary>Repositorio del agregado AgendaEmpleado (horarios de empleados).</summary>
public interface IAgendaEmpleadoRepository
{
    Task<AgendaEmpleado?> ObtenerPorIdAsync(int idHorario, CancellationToken cancellationToken = default);
}
