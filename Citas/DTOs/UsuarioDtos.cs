namespace Citas.DTOs;

public class CreateUsuarioDto
{
    public string NombreUsuario { get; set; } = string.Empty;
}

public class UsuarioResponseDto
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
}
