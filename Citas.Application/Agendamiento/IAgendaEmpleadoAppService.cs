namespace Citas.Application.Agendamiento;

public interface IAgendaEmpleadoAppService
{
    /// <summary>Ajusta la hora de fin de un horario. Devuelve false si no existe.</summary>
    Task<bool> ActualizarAsync(int idHorario, UpdateAgendaEmpleadoDto dto, CancellationToken cancellationToken = default);
}
