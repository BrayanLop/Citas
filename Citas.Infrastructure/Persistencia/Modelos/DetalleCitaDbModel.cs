using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citas.Infrastructure.Persistencia.Modelos;

[Table("DetalleCitas")]
public class DetalleCitaDbModel : ITenantDbModel
{
    [Key]
    public int IdDetalle { get; set; }

    public int IdCita { get; set; }
    public int IdServicio { get; set; }

    // Precisión (18,2) configurada por Fluent en el DbContext.
    public decimal ValorServicio { get; set; }

    public int TiempoEstimado { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    public CitaDbModel? Cita { get; set; }
    public ServicioDbModel? Servicio { get; set; }
}
