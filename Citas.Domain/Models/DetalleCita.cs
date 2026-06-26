namespace Citas.Models;

// Línea de detalle de una cita (un servicio prestado dentro de la cita).
// Forma parte del agregado Cita.
public class DetalleCita
{
    public int IdDetalle { get; set; }
    public int IdCita { get; set; }
    public int IdServicio { get; set; }
    public decimal ValorServicio { get; set; }
    public int TiempoEstimado { get; set; }
}
