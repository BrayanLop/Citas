namespace Citas.Application.Servicios;

public interface IServicioAppService
{
    Task<IReadOnlyList<ServicioResponseDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<ServicioResponseDto> CrearAsync(CreateServicioDto dto, CancellationToken cancellationToken = default);

    /// <summary>Devuelve false si el servicio no existe.</summary>
    Task<bool> ActualizarAsync(int idServicio, UpdateServicioDto dto, CancellationToken cancellationToken = default);

    /// <summary>Devuelve false si el servicio no existe.</summary>
    Task<bool> EliminarAsync(int idServicio, CancellationToken cancellationToken = default);
}
