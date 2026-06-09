using Citas.Models;

namespace Citas.Domain.Empresas;

/// <summary>Repositorio del agregado Empresa (catálogo de tenants).</summary>
public interface IEmpresaRepository
{
    Task<IReadOnlyList<Empresa>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Empresa?> ObtenerPorIdAsync(string id, CancellationToken cancellationToken = default);

    Task<bool> ExisteAsync(string id, CancellationToken cancellationToken = default);

    Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default);
}
