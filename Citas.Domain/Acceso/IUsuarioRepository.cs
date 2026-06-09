using Citas.Models;

namespace Citas.Domain.Acceso;

/// <summary>Repositorio del agregado Usuario (contexto Acceso).</summary>
public interface IUsuarioRepository
{
    /// <summary>Usuarios del tenant actual con sus roles cargados.</summary>
    Task<IReadOnlyList<Usuario>> ListarConRolesAsync(CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    void Eliminar(Usuario usuario);

    /// <summary>
    /// Busca el usuario de una persona en un tenant concreto, ignorando el filtro
    /// global de tenant (se usa en el flujo de login → elegir empresa).
    /// </summary>
    Task<Usuario?> BuscarEnTenantPorPersonaAsync(int personaId, string tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca un usuario por nombre dentro de un tenant concreto, ignorando el filtro
    /// global de tenant (puente de empleado Natural → Citas).
    /// </summary>
    Task<Usuario?> BuscarEnTenantPorNombreAsync(string tenant, string nombre, CancellationToken cancellationToken = default);
}
