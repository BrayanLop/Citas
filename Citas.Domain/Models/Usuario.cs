using Citas.Domain.Common;

namespace Citas.Models;

// Usuario dentro de una empresa. Puede venir de una Persona global (cliente que se
// registró) o existir solo en la empresa (empleado dado de alta en el tenant).
public class Usuario
{
    private Usuario() { }

    public int Id { get; private set; }
    public string NombreUsuario { get; private set; } = string.Empty;
    public string? ClaveHash { get; private set; }
    public bool? Estado { get; private set; } = true;
    public int? PersonaId { get; private set; }

    // Roles del usuario; el mapper los rellena cuando se necesitan (p. ej. listado con rol).
    public ICollection<RolUsuario> RolUsuarios { get; private set; } = [];

    // Usuario suelto, sin persona global detrás (el caso del empleado).
    public static Usuario Crear(string nombreUsuario)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            throw new DomainException("El nombre de usuario es obligatorio.");
        return new Usuario { NombreUsuario = nombreUsuario.Trim(), Estado = true };
    }

    // Usuario ligado a una persona global (el cliente al elegir empresa).
    public static Usuario CrearParaPersona(string nombreUsuario, int personaId)
    {
        var usuario = Crear(nombreUsuario);
        usuario.PersonaId = personaId;
        return usuario;
    }

    // La persistencia asigna el id real una sola vez, tras el primer guardado.
    public void EstablecerIdentidad(int id)
    {
        if (Id == 0) Id = id;
    }

    // Reconstruye un usuario existente desde la base de datos.
    public static Usuario Rehidratar(
        int id,
        string nombreUsuario,
        string? claveHash,
        bool? estado,
        int? personaId,
        IEnumerable<RolUsuario>? roles = null)
    {
        var usuario = new Usuario
        {
            Id = id,
            NombreUsuario = nombreUsuario,
            ClaveHash = claveHash,
            Estado = estado,
            PersonaId = personaId,
        };
        if (roles is not null)
            usuario.RolUsuarios = roles.ToList();
        return usuario;
    }
}
