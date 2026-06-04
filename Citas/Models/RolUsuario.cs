namespace Citas.Models;

public class RolUsuario
{
    public int IdRolUsuario { get; set; }
    public int IdUsuario { get; set; }
    public int IdRol { get; set; }

    public Usuario? Usuario { get; set; }
    public Rol? Rol { get; set; }
}
