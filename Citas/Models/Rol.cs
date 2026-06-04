namespace Citas.Models;

public class Rol
{
    public int Id { get; set; }
    public string NombreRol { get; set; } = string.Empty;

    public ICollection<RolUsuario> RolUsuarios { get; set; } = [];
}
