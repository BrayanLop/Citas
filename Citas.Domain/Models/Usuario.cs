using Citas.Domain.Common;

namespace Citas.Models;

/// <summary>
/// Usuario dentro de un tenant (contexto Acceso). Puede estar ligado a una
/// <see cref="Persona"/> global (cliente) o existir solo en el tenant (empleado).
/// </summary>
public class Usuario : ITenantEntity
{
    // Constructor sin parámetros para EF Core.
    private Usuario() { }

    public int Id { get; private set; }
    public string NombreUsuario { get; private set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string? ClaveHash { get; private set; }
    public bool? Estado { get; private set; } = true;
    public int? PersonaId { get; private set; }

    public Persona? Persona { get; private set; }

    public ICollection<RolUsuario> RolUsuarios { get; private set; } = [];
    public ICollection<Cita> CitasCliente { get; private set; } = [];
    public ICollection<Cita> CitasEmpleado { get; private set; } = [];
    public ICollection<AgendaEmpleado> AgendaEmpleado { get; private set; } = [];

    /// <summary>Crea un usuario simple (sin persona global asociada).</summary>
    public static Usuario Crear(string nombreUsuario)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            throw new DomainException("El nombre de usuario es obligatorio.");
        return new Usuario { NombreUsuario = nombreUsuario.Trim(), Estado = true };
    }

    /// <summary>Crea un usuario (cliente) ligado a una persona global.</summary>
    public static Usuario CrearParaPersona(string nombreUsuario, int personaId)
    {
        var usuario = Crear(nombreUsuario);
        usuario.PersonaId = personaId;
        return usuario;
    }
}
