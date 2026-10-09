using Citas.Models;

namespace Citas.Domain.Servicios;

/// <summary>
/// Repositorio del agregado Servicio (catálogo por tenant). Los servicios nacen en Natural
/// y llegan por sincronización: no hay borrado (pueden tener citas históricas).
/// </summary>
public interface IServicioRepository
{
    /// <summary>Todos los servicios del tenant actual (agendables o no).</summary>
    Task<IReadOnlyList<Servicio>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Servicio?> ObtenerPorIdAsync(int idServicio, CancellationToken cancellationToken = default);

    /// <summary>Servicios del tenant actual con esos ids (los que no existan no vienen).</summary>
    Task<IReadOnlyList<Servicio>> ObtenerPorIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);

    Task AgregarAsync(Servicio servicio, CancellationToken cancellationToken = default);

    /// <summary>Persiste los cambios de un servicio ya existente.</summary>
    void Actualizar(Servicio servicio);
}
