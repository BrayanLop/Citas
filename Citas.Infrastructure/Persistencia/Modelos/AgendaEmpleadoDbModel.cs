using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citas.Infrastructure.Persistencia.Modelos;

[Table("AgendaEmpleados")]
public class AgendaEmpleadoDbModel : ITenantDbModel
{
    [Key]
    public int IdHorario { get; set; }

    public int IdUsuario { get; set; }
    public DateTime FechaReservada { get; set; }
    public TimeSpan HoraEstimadaCita { get; set; }
    public TimeSpan HoraEstimadaFin { get; set; }

    [Required]
    [MaxLength(50)]
    public string EstadoHorario { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    public UsuarioDbModel? Usuario { get; set; }
}
