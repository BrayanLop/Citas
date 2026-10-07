using Citas.Domain.Common;

namespace Citas.Models;

// Usuario dentro de una empresa. Puede venir de una Persona global (cliente que se
// registró en Citas), de una persona de Natural (personal de la empresa, vinculado por
// PersonaNaturalId) o existir solo en la empresa (alta manual en el tenant).
public class Usuario
{
    private Usuario() { }

    public int Id { get; private set; }
    public string NombreUsuario { get; private set; } = string.Empty;
    public string? ClaveHash { get; private set; }
    public bool? Estado { get; private set; } = true;
    public int? PersonaId { get; private set; }

    // Id de la persona en Natural cuando el usuario es personal de la empresa (barbero,
    // administrador...). Es único por tenant y es lo único que lo vincula con Natural.
    public int? PersonaNaturalId { get; private set; }

    public bool EsPersonalNatural => PersonaNaturalId.HasValue;
    public bool EstaActivo => Estado != false;

    // Roles del usuario; el mapper los rellena cuando se necesitan (p. ej. listado con rol).
    public ICollection<RolUsuario> RolUsuarios { get; private set; } = [];

    // Usuario suelto, sin persona global detrás (alta manual en el tenant).
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

    // Usuario que representa a personal de Natural; se crea solo la primera vez que esa
    // persona entra a Citas con su token de Natural.
    public static Usuario CrearParaPersonalNatural(string nombreUsuario, int personaNaturalId)
    {
        if (personaNaturalId <= 0)
            throw new DomainException("La persona de Natural no es válida.");
        var usuario = Crear(nombreUsuario);
        usuario.PersonaNaturalId = personaNaturalId;
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
        int? personaNaturalId,
        IEnumerable<RolUsuario>? roles = null)
    {
        var usuario = new Usuario
        {
            Id = id,
            NombreUsuario = nombreUsuario,
            ClaveHash = claveHash,
            Estado = estado,
            PersonaId = personaId,
            PersonaNaturalId = personaNaturalId,
        };
        if (roles is not null)
            usuario.RolUsuarios = roles.ToList();
        return usuario;
    }
}
