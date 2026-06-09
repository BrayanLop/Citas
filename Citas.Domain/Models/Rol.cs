using Citas.Domain.Common;

namespace Citas.Models;

/// <summary>Rol dentro de un tenant (contexto Acceso).</summary>
public class Rol : ITenantEntity
{
    // Constructor sin parámetros para EF Core.
    private Rol() { }

    public int Id { get; private set; }
    public string NombreRol { get; private set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;

    public ICollection<RolUsuario> RolUsuarios { get; private set; } = [];

    public static Rol Crear(string nombreRol)
    {
        if (string.IsNullOrWhiteSpace(nombreRol))
            throw new DomainException("El nombre del rol es obligatorio.");
        return new Rol { NombreRol = nombreRol.Trim() };
    }
}
