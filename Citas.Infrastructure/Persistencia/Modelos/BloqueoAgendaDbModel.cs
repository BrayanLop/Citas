using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citas.Infrastructure.Persistencia.Modelos;

// Bloqueo/ausencia de un empleado: [Inicio, Fin) en UTC (timestamptz).
[Table("BloqueosAgenda")]
public class BloqueoAgendaDbModel : ITenantDbModel
{
    [Key]
    public int IdBloqueo { get; set; }

    public int IdEmpleado { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }

    [MaxLength(200)]
    public string? Motivo { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    public UsuarioDbModel? Empleado { get; set; }
}
