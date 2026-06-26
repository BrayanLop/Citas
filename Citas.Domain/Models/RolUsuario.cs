namespace Citas.Models;

// Relación rol ↔ usuario dentro de una empresa.
public class RolUsuario
{
    private RolUsuario() { }

    public int IdRolUsuario { get; private set; }
    public int IdUsuario { get; private set; }
    public int IdRol { get; private set; }

    // El rol asociado; lo rellena el mapper cuando se carga el usuario con sus roles.
    public Rol? Rol { get; private set; }

    public static RolUsuario Crear(int idUsuario, int idRol)
        => new() { IdUsuario = idUsuario, IdRol = idRol };

    public static RolUsuario Rehidratar(int idRolUsuario, int idUsuario, int idRol, Rol? rol = null)
        => new()
        {
            IdRolUsuario = idRolUsuario,
            IdUsuario = idUsuario,
            IdRol = idRol,
            Rol = rol,
        };
}
