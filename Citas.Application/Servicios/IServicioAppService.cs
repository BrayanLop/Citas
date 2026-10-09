namespace Citas.Application.Servicios;

/// <summary>
/// Catálogo de servicios del tenant. Los servicios se crean en Natural y llegan por
/// sincronización (POST /Sincronizacion/Natural); aquí solo se consultan y se ajustan.
/// </summary>
public interface IServicioAppService
{
    /// <summary>Cliente: solo los agendables. Personal: todos, indicando cuáles no lo son.</summary>
    Task<IReadOnlyList<ServicioResponseDto>> ListarAsync(CancellationToken cancellationToken = default);

    /// <summary>Ajusta duración y/o disponibilidad. Devuelve false si el servicio no existe.</summary>
    Task<bool> ActualizarAsync(int idServicio, UpdateServicioDto dto, CancellationToken cancellationToken = default);
}
