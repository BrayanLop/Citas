namespace Citas.Application.Agendamiento;

public class UpdateAgendaEmpleadoDto
{
    public TimeSpan? HoraEstimadaFin { get; set; }
}

public class AgendaEmpleadoResponseDto
{
    public int IdHorario { get; set; }
    public int IdUsuario { get; set; }
    public DateTime FechaReservada { get; set; }
    public TimeSpan HoraEstimadaCita { get; set; }
    public TimeSpan HoraEstimadaFin { get; set; }
    public string EstadoHorario { get; set; } = string.Empty;
}
