using Citas.Domain.Common;

namespace Citas.Models;

// Franja horaria reservada en la agenda de un empleado.
public class AgendaEmpleado
{
    private AgendaEmpleado() { }

    public int IdHorario { get; private set; }
    public int IdUsuario { get; private set; }
    public DateTime FechaReservada { get; private set; }
    public TimeSpan HoraEstimadaCita { get; private set; }
    public TimeSpan HoraEstimadaFin { get; private set; }
    public string EstadoHorario { get; private set; } = "Disponible";

    public static AgendaEmpleado Rehidratar(
        int idHorario,
        int idUsuario,
        DateTime fechaReservada,
        TimeSpan horaInicio,
        TimeSpan horaFin,
        string estadoHorario)
        => new()
        {
            IdHorario = idHorario,
            IdUsuario = idUsuario,
            FechaReservada = fechaReservada,
            HoraEstimadaCita = horaInicio,
            HoraEstimadaFin = horaFin,
            EstadoHorario = estadoHorario,
        };

    // Mueve la hora de fin, siempre que quede después del inicio.
    public void AjustarHoraFin(TimeSpan horaFin)
    {
        if (horaFin <= HoraEstimadaCita)
            throw new DomainException("La hora de fin debe ser posterior a la de inicio.");
        HoraEstimadaFin = horaFin;
    }
}
