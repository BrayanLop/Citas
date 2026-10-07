using Citas.Models;

namespace Citas.Domain.Empresas;

/// <summary>Repositorio del agregado Empresa (catálogo de tenants).</summary>
public interface IEmpresaRepository
{
    /// <summary>Empresas activas (las que pueden recibir citas).</summary>
    Task<IReadOnlyList<Empresa>> ListarActivasAsync(CancellationToken cancellationToken = default);

    Task<Empresa?> ObtenerPorIdAsync(string id, CancellationToken cancellationToken = default);

    Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default);

    /// <summary>Persiste los cambios de una empresa ya existente.</summary>
    void Actualizar(Empresa empresa);
}
