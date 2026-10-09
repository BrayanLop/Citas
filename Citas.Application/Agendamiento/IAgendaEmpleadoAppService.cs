namespace Citas.Application.Agendamiento;

/// <summary>
/// Horario semanal y bloqueos de los empleados. Escribir: personal '01'/'03' sobre cualquier
/// empleado; '02' solo sobre su propia agenda.
/// </summary>
public interface IAgendaEmpleadoAppService
{
    /// <summary>Horario de un empleado activo (404 si no lo es).</summary>
    Task<HorarioEmpleadoDto> ObtenerHorarioAsync(int usuarioId, CancellationToken cancellationToken = default);

    /// <summary>Reemplaza el horario semanal completo.</summary>
    Task<HorarioEmpleadoDto> ReemplazarHorarioAsync(int usuarioId, ActualizarHorarioDto dto, CancellationToken cancellationToken = default);

    /// <summary>Bloqueos que se cruzan con los días locales [desde, hasta] (por defecto, desde hoy 31 días).</summary>
    Task<IReadOnlyList<BloqueoDto>> ListarBloqueosAsync(
        int usuarioId,
        DateOnly? desde,
        DateOnly? hasta,
        CancellationToken cancellationToken = default);

    /// <summary>Crea un bloqueo; 409 si se cruza con citas activas del empleado.</summary>
    Task<BloqueoDto> CrearBloqueoAsync(int usuarioId, CrearBloqueoDto dto, CancellationToken cancellationToken = default);

    /// <summary>Elimina un bloqueo. Devuelve false si no existe.</summary>
    Task<bool> EliminarBloqueoAsync(int usuarioId, int idBloqueo, CancellationToken cancellationToken = default);
}
