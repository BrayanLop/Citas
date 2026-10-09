namespace Citas.Application.Acceso;

public class CreateUsuarioDto
{
    public string NombreUsuario { get; set; } = string.Empty;
}

public class UsuarioResponseDto
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public int? PersonaId { get; set; }
    /// <summary>Id de la persona en Natural cuando el usuario es personal de la empresa.</summary>
    public int? PersonaNaturalId { get; set; }
    /// <summary>Nombre del rol principal del usuario (ej: "Cliente", "Empleado").</summary>
    public string? Rol { get; set; }
    /// <summary>False si el usuario quedó inactivo (p. ej. ya no está en Natural).</summary>
    public bool Activo { get; set; }
    /// <summary>Contacto del personal (copiado de Natural); no se muestra a los clientes.</summary>
    public string? Email { get; set; }
    public string? Celular { get; set; }
}
