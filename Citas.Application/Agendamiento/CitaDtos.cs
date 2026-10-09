namespace Citas.Application.Agendamiento;

public class CreateCitaDto
{
    public int IdCliente { get; set; }
    public int IdEmpleado { get; set; }
    public DateTime FechaCita { get; set; }
    public TimeSpan HoraEstimadaCita { get; set; }

    /// <summary>Se ignora si la cita lleva servicios: entonces fin = inicio + suma de sus duraciones.</summary>
    public TimeSpan? HoraEstimadaFin { get; set; }

    public string? Observaciones { get; set; }

    /// <summary>Servicios de la cita (ids de Citas, agendables y del tenant). Opcional.</summary>
    public List<int>? IdServicios { get; set; }
}

public class UpdateCitaDto
{
    public string? Estado { get; set; }
    public DateTime? FechaCita { get; set; }
    public TimeSpan? HoraEstimadaFin { get; set; }
}

public class PartialUpdateCitaDto
{
    public string? Estado { get; set; }
}

public class DetalleCitaDto
{
    public int IdServicio { get; set; }
    public string? NombreServicio { get; set; }
    public decimal ValorServicio { get; set; }
    public int TiempoEstimado { get; set; }
}

public class CitaResponseDto
{
    public int IdCita { get; set; }
    public int IdCliente { get; set; }
    public int IdEmpleado { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCita { get; set; }
    public TimeSpan HoraEstimadaCita { get; set; }
    public TimeSpan? HoraEstimadaFin { get; set; }
    public string? Observaciones { get; set; }
    public string? NombreCliente { get; set; }
    public string? NombreEmpleado { get; set; }
    public List<DetalleCitaDto> Servicios { get; set; } = [];

    /// <summary>Suma de las duraciones de los servicios (minutos).</summary>
    public int DuracionTotal { get; set; }

    public decimal ValorTotal { get; set; }
}
