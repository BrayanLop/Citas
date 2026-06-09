namespace Citas.Application.Agendamiento;

public class CreateCitaDto
{
    public int IdCliente { get; set; }
    public int IdEmpleado { get; set; }
    public DateTime FechaCita { get; set; }
    public TimeSpan HoraEstimadaCita { get; set; }
    public TimeSpan? HoraEstimadaFin { get; set; }
    public string? Observaciones { get; set; }
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
}
