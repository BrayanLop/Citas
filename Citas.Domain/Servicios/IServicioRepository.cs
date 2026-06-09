using Citas.Models;

namespace Citas.Domain.Servicios;

/// <summary>Repositorio del agregado Servicio (catálogo por tenant).</summary>
public interface IServicioRepository
{
    Task<IReadOnlyList<Servicio>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Servicio?> ObtenerPorIdAsync(int idServicio, CancellationToken cancellationToken = default);

    Task AgregarAsync(Servicio servicio, CancellationToken cancellationToken = default);

    void Eliminar(Servicio servicio);
}
