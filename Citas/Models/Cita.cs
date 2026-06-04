namespace Citas.Models;

public class Cita : ITenantEntity
{
    public int IdCita { get; set; }
    public int IdCliente { get; set; }
    public int IdEmpleado { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public DateTime FechaCita { get; set; }
    public TimeSpan HoraEstimadaCita { get; set; }
    public TimeSpan? HoraEstimadaFin { get; set; }
    public string? Observaciones { get; set; }
    public string TenantId { get; set; } = string.Empty;

    public Usuario? Cliente { get; set; }
    public Usuario? Empleado { get; set; }
    public ICollection<DetalleCita> DetalleCitas { get; set; } = [];
}
