namespace Citas.Models;

// Línea de detalle de una cita (un servicio prestado dentro de la cita). Forma parte del
// agregado Cita: guarda una foto del valor y la duración del servicio al agendar, para
// que la cita no cambie si luego se ajusta el catálogo.
public class DetalleCita
{
    private DetalleCita() { }

    public int IdDetalle { get; private set; }
    public int IdServicio { get; private set; }
    public decimal ValorServicio { get; private set; }
    public int TiempoEstimado { get; private set; }

    // Nombre del servicio para mostrar; el mapper lo rellena al leer si lo tiene.
    public string? NombreServicio { get; private set; }

    public static DetalleCita DeServicio(Servicio servicio)
        => new()
        {
            IdServicio = servicio.IdServicio,
            ValorServicio = servicio.Valor,
            TiempoEstimado = servicio.TiempoEstimado,
            NombreServicio = servicio.NombreServicio,
        };

    public static DetalleCita Rehidratar(
        int idDetalle,
        int idServicio,
        decimal valorServicio,
        int tiempoEstimado,
        string? nombreServicio)
        => new()
        {
            IdDetalle = idDetalle,
            IdServicio = idServicio,
            ValorServicio = valorServicio,
            TiempoEstimado = tiempoEstimado,
            NombreServicio = nombreServicio,
        };
}
