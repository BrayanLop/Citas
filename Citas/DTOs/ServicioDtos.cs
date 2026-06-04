namespace Citas.DTOs;

public class CreateServicioDto
{
    public string NombreServicio { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int TiempoEstimado { get; set; }
}

public class UpdateServicioDto
{
    public string NombreServicio { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int TiempoEstimado { get; set; }
}

public class ServicioResponseDto
{
    public int IdServicio { get; set; }
    public string NombreServicio { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int TiempoEstimado { get; set; }
}
