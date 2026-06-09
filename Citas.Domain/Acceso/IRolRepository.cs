using Citas.Models;

namespace Citas.Domain.Acceso;

/// <summary>Repositorio de roles y sus asignaciones (contexto Acceso).</summary>
public interface IRolRepository
{
    /// <summary>Roles del tenant actual.</summary>
    Task<IReadOnlyList<Rol>> ListarAsync(CancellationToken cancellationToken = default);

    /// <summary>Busca un rol por nombre en un tenant concreto, ignorando el filtro global.</summary>
    Task<Rol?> BuscarEnTenantPorNombreAsync(string tenant, string nombreRol, CancellationToken cancellationToken = default);

    Task AgregarAsync(Rol rol, CancellationToken cancellationToken = default);

    /// <summary>Indica si ya existe la asignación rol↔usuario, ignorando el filtro global.</summary>
    Task<bool> ExisteAsignacionAsync(int usuarioId, int rolId, CancellationToken cancellationToken = default);

    Task AgregarAsignacionAsync(RolUsuario asignacion, CancellationToken cancellationToken = default);
}
