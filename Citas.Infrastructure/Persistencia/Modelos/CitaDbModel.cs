using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citas.Infrastructure.Persistencia.Modelos;

[Table("Citas")]
public class CitaDbModel : ITenantDbModel
{
    [Key]
    public int IdCita { get; set; }

    public int IdCliente { get; set; }
    public int IdEmpleado { get; set; }

    [Required]
    [MaxLength(50)]
    public string Estado { get; set; } = string.Empty;

    // Instantes en UTC (timestamptz). La fecha/hora local del negocio se calcula al leer.
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }

    public string? Observaciones { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    public UsuarioDbModel? Cliente { get; set; }
    public UsuarioDbModel? Empleado { get; set; }
    public ICollection<DetalleCitaDbModel> DetalleCitas { get; set; } = [];
}
