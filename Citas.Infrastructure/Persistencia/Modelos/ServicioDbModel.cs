using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citas.Infrastructure.Persistencia.Modelos;

[Table("Servicios")]
public class ServicioDbModel : ITenantDbModel
{
    [Key]
    public int IdServicio { get; set; }

    [Required]
    [MaxLength(150)]
    public string NombreServicio { get; set; } = string.Empty;

    // La precisión (18,2) se configura por Fluent en el DbContext.
    public decimal Valor { get; set; }

    public int TiempoEstimado { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    public ICollection<DetalleCitaDbModel> DetalleCitas { get; set; } = [];
}
