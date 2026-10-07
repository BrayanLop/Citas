using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citas.Infrastructure.Persistencia.Modelos;

[Table("Usuarios")]
public class UsuarioDbModel : ITenantDbModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    public string? ClaveHash { get; set; }

    // El default (true) lo pone la base de datos; ver la config en el DbContext.
    public bool? Estado { get; set; }

    public int? PersonaId { get; set; }

    // Persona de Natural a la que representa (solo personal). Único por tenant.
    public int? PersonaNaturalId { get; set; }

    public PersonaDbModel? Persona { get; set; }
    public ICollection<RolUsuarioDbModel> RolUsuarios { get; set; } = [];
    public ICollection<CitaDbModel> CitasCliente { get; set; } = [];
    public ICollection<CitaDbModel> CitasEmpleado { get; set; } = [];
    public ICollection<AgendaEmpleadoDbModel> AgendaEmpleado { get; set; } = [];
}
