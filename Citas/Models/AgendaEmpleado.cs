namespace Citas.Models;

public class AgendaEmpleado
{
    public int IdHorario { get; set; }
    public int IdUsuario { get; set; }
    public DateTime FechaReservada { get; set; }
    public TimeSpan HoraEstimadaCita { get; set; }
    public TimeSpan HoraEstimadaFin { get; set; }
    public string EstadoHorario { get; set; } = "Disponible";

    public Usuario? Usuario { get; set; }
}
