using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citas.Infrastructure.Persistencia.Modelos;

// Una fila por franja del horario semanal de un empleado (hora local del negocio).
// DiaSemana: 0 = domingo ... 6 = sábado (System.DayOfWeek).
[Table("AgendaEmpleados")]
public class AgendaEmpleadoDbModel : ITenantDbModel
{
    [Key]
    public int IdFranja { get; set; }

    public int IdUsuario { get; set; }
    public int DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    public UsuarioDbModel? Usuario { get; set; }
}
