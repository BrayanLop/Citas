namespace Citas.Models;

public class RolUsuario : ITenantEntity
{
    public int IdRolUsuario { get; set; }
    public int IdUsuario { get; set; }
    public int IdRol { get; set; }
    public string TenantId { get; set; } = string.Empty;

    public Usuario? Usuario { get; set; }
    public Rol? Rol { get; set; }
}
