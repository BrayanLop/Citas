namespace Citas.Models;

public class DetalleCita : ITenantEntity
{
    public int IdDetalle { get; set; }
    public int IdCita { get; set; }
    public int IdServicio { get; set; }
    public decimal ValorServicio { get; set; }
    public int TiempoEstimado { get; set; }
    public string TenantId { get; set; } = string.Empty;

    public Cita? Cita { get; set; }
    public Servicio? Servicio { get; set; }
}
