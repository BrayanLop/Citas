namespace Citas.Models;

/// <summary>Asignación de un <see cref="Rol"/> a un <see cref="Usuario"/> dentro de un tenant.</summary>
public class RolUsuario : ITenantEntity
{
    // Constructor sin parámetros para EF Core.
    private RolUsuario() { }

    public int IdRolUsuario { get; private set; }
    public int IdUsuario { get; private set; }
    public int IdRol { get; private set; }
    public string TenantId { get; set; } = string.Empty;

    public Usuario? Usuario { get; private set; }
    public Rol? Rol { get; private set; }

    public static RolUsuario Crear(int idUsuario, int idRol)
        => new() { IdUsuario = idUsuario, IdRol = idRol };
}
