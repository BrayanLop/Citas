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

    // Contacto del personal, copiado de Natural al sincronizar (los clientes lo tienen en Persona).
    public string? Email { get; private set; }
    public string? Celular { get; private set; }

    public bool EsPersonalNatural => PersonaNaturalId.HasValue;
    public bool EstaActivo => Estado != false;

    /// <summary>
    /// Copia los datos de la persona de Natural (nombre completo y contacto) y reactiva al
    /// usuario si estaba inactivo. Devuelve true si algo cambió.
    /// </summary>
    public bool SincronizarDesdeNatural(string nombreCompleto, string? email, string? celular)
    {
        if (!EsPersonalNatural)
            throw new DomainException("Solo el personal de Natural se sincroniza con Natural.");

        var nombre = string.IsNullOrWhiteSpace(nombreCompleto) ? NombreUsuario : nombreCompleto.Trim();
        if (nombre.Length > LongitudMaximaNombre)
            nombre = nombre[..LongitudMaximaNombre];
        email = Limpiar(email, 150);
        celular = Limpiar(celular, 30);

        var cambio = nombre != NombreUsuario || email != Email || celular != Celular || !EstaActivo;
        NombreUsuario = nombre;
        Email = email;
        Celular = celular;
        Estado = true;
        return cambio;
    }

    /// <summary>Deja al usuario inactivo (no se borra). Devuelve true si estaba activo.</summary>
    public bool Desactivar()
    {
        if (!EstaActivo)
            return false;
        Estado = false;
        return true;
    }

    private const int LongitudMaximaNombre = 100;

    private static string? Limpiar(string? valor, int longitudMaxima)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        valor = valor.Trim();
        return valor.Length > longitudMaxima ? valor[..longitudMaxima] : valor;
    }

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
        string? email = null,
        string? celular = null,
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
            Email = email,
            Celular = celular,
        };
        if (roles is not null)
            usuario.RolUsuarios = roles.ToList();
        return usuario;
    }
}
