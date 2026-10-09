namespace Citas.Application.Agendamiento;

/// <summary>
/// Casos de uso del agregado Cita. Orquesta el dominio y la persistencia; los
/// controladores delegan aquí en lugar de contener lógica de negocio.
/// </summary>
public interface ICitaAppService
{
    Task<IReadOnlyList<CitaResponseDto>> ListarAsync(
        int? clienteId = null,
        int? empleadoId = null,
        CancellationToken cancellationToken = default);

    Task<CitaResponseDto> AgendarAsync(CreateCitaDto dto, CancellationToken cancellationToken = default);

    /// <summary>Reprograma (fecha/hora, empleado, servicios, observaciones). Null si la cita no existe.</summary>
    Task<CitaResponseDto?> ReprogramarAsync(int idCita, UpdateCitaDto dto, CancellationToken cancellationToken = default);

    /// <summary>Cambia solo el estado. Devuelve false si la cita no existe.</summary>
    Task<bool> CambiarEstadoAsync(int idCita, PartialUpdateCitaDto dto, CancellationToken cancellationToken = default);

    /// <summary>Elimina la cita. Devuelve false si no existe.</summary>
    Task<bool> EliminarAsync(int idCita, CancellationToken cancellationToken = default);
}
