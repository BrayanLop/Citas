namespace Citas.Models;

public class Servicio
{
    public int IdServicio { get; set; }
    public string NombreServicio { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int TiempoEstimado { get; set; }

    public ICollection<DetalleCita> DetalleCitas { get; set; } = [];
}
