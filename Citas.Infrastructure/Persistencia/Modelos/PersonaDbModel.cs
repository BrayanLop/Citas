using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citas.Infrastructure.Persistencia.Modelos;

// Identidad global del cliente externo (login propio de Citas). No es un tenant.
// La tabla la crea la migración InitialCreate como el resto; el email es único
// (índice configurado en el DbContext).
[Table("Personas")]
public class PersonaDbModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Apellido { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Cedula { get; set; }

    [MaxLength(200)]
    public string? Residencia { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    public int? Edad { get; set; }

    [MaxLength(30)]
    public string? Celular { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    // Hash PBKDF2 de la contraseña del login global.
    [MaxLength(200)]
    public string? ClaveHash { get; set; }

    public ICollection<UsuarioDbModel> Usuarios { get; set; } = [];
}
