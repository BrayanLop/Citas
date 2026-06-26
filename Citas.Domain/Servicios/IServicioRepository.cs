using Citas.Models;

namespace Citas.Domain.Servicios;

/// <summary>Repositorio del agregado Servicio (catálogo por tenant).</summary>
public interface IServicioRepository
{
    Task<IReadOnlyList<Servicio>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Servicio?> ObtenerPorIdAsync(int idServicio, CancellationToken cancellationToken = default);

    Task AgregarAsync(Servicio servicio, CancellationToken cancellationToken = default);

    /// <summary>Persiste los cambios de un servicio ya existente.</summary>
    void Actualizar(Servicio servicio);

    void Eliminar(Servicio servicio);
}
