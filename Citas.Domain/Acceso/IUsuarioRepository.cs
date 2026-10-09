using Citas.Models;

namespace Citas.Domain.Acceso;

/// <summary>Repositorio del agregado Usuario (contexto Acceso).</summary>
public interface IUsuarioRepository
{
    /// <summary>Usuarios del tenant actual con sus roles cargados.</summary>
    Task<IReadOnlyList<Usuario>> ListarConRolesAsync(CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Da de alta un usuario. Si <paramref name="tenant"/> viene informado, se sella ese
    /// tenant explícitamente (útil en el login, donde el token aún no trae empresa). Si es
    /// null, lo asigna el contexto a partir del tenant actual.
    /// </summary>
    Task AgregarAsync(Usuario usuario, string? tenant, CancellationToken cancellationToken = default);

    void Eliminar(Usuario usuario);

    /// <summary>Persiste los cambios de un usuario ya existente.</summary>
    void Actualizar(Usuario usuario);

    /// <summary>Personal de Natural del tenant actual (activo o no).</summary>
    Task<IReadOnlyList<Usuario>> ListarPersonalNaturalAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca el usuario de una persona en un tenant concreto, ignorando el filtro
    /// global de tenant (se usa en el flujo de login → elegir empresa).
    /// </summary>
    Task<Usuario?> BuscarEnTenantPorPersonaAsync(int personaId, string tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca el usuario que representa a una persona de Natural (personal) en un tenant
    /// concreto. La vinculación es siempre por el id de Natural, nunca por nombre.
    /// </summary>
    Task<Usuario?> BuscarEnTenantPorPersonaNaturalAsync(string tenant, int personaNaturalId, CancellationToken cancellationToken = default);
}
