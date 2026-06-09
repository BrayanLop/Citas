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
    /// <summary>Nombre del rol principal del usuario (ej: "Cliente", "Empleado").</summary>
    public string? Rol { get; set; }
}
