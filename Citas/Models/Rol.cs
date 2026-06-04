namespace Citas.Models;

public class Rol : ITenantEntity
{
    public int Id { get; set; }
    public string NombreRol { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;

    public ICollection<RolUsuario> RolUsuarios { get; set; } = [];
}
