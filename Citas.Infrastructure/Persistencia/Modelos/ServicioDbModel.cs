using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citas.Infrastructure.Persistencia.Modelos;

[Table("Servicios")]
public class ServicioDbModel : ITenantDbModel
{
    [Key]
    public int IdServicio { get; set; }

    // Id del servicio en Natural. Único por tenant (índice en el DbContext).
    public int ServicioNaturalId { get; set; }

    [Required]
    [MaxLength(150)]
    public string NombreServicio { get; set; } = string.Empty;

    // La precisión (18,2) se configura por Fluent en el DbContext.
    public decimal Valor { get; set; }

    // Duración en minutos (propia de Citas).
    public int TiempoEstimado { get; set; }

    // Disponibilidad para agendar decidida en Citas.
    public bool Activo { get; set; }

    // Sigue en Natural y allí está disponible (lo fija la sincronización).
    public bool DisponibleEnNatural { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    public ICollection<DetalleCitaDbModel> DetalleCitas { get; set; } = [];
}
