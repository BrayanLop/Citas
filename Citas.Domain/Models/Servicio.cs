namespace Citas.Models;

public class Servicio : ITenantEntity
{
    public int IdServicio { get; set; }
    public string NombreServicio { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int TiempoEstimado { get; set; }
    public string TenantId { get; set; } = string.Empty;

    public ICollection<DetalleCita> DetalleCitas { get; set; } = [];
}
