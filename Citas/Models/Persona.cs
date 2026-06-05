namespace Citas.Models;

/// <summary>
/// Datos personales asociados a un usuario. La tabla fue creada manualmente con
/// nombres en minuscula (personas, id, nombre, ...), por eso se mapea de forma
/// explicita en OnModelCreating. NO pertenece a un tenant (no tiene TenantId).
/// </summary>
public class Persona
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string? Cedula { get; set; }
    public string? Residencia { get; set; }
    public string? Email { get; set; }
    public int? Edad { get; set; }
    public string? Celular { get; set; }
    public DateTime? FechaNacimiento { get; set; }

    public ICollection<Usuario> Usuarios { get; set; } = [];
}
