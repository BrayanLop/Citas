using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citas.Infrastructure.Persistencia.Modelos;

// Identidad global del cliente. La tabla "Personas" se creó a mano con las columnas
// en minúscula, por eso cada propiedad lleva su [Column] explícito. No es un tenant.
[Table("Personas")]
public class PersonaDbModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [Column("apellido")]
    public string Apellido { get; set; } = string.Empty;

    [Column("cedula")]
    public string? Cedula { get; set; }

    [Column("residencia")]
    public string? Residencia { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("edad")]
    public int? Edad { get; set; }

    [Column("celular")]
    public string? Celular { get; set; }

    [Column("fechanacimiento")]
    public DateTime? FechaNacimiento { get; set; }

    // El hash sí quedó con C mayúscula en la columna real.
    [Column("ClaveHash")]
    public string? ClaveHash { get; set; }

    public ICollection<UsuarioDbModel> Usuarios { get; set; } = [];
}
