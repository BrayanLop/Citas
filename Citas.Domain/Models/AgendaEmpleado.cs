using Citas.Domain.Common;

namespace Citas.Models;

/// <summary>
/// Franja horaria reservada en la agenda de un empleado (contexto Agendamiento).
/// </summary>
public class AgendaEmpleado : ITenantEntity
{
    // Constructor sin parámetros para EF Core.
    private AgendaEmpleado() { }

    public int IdHorario { get; private set; }
    public int IdUsuario { get; private set; }
    public DateTime FechaReservada { get; private set; }
    public TimeSpan HoraEstimadaCita { get; private set; }
    public TimeSpan HoraEstimadaFin { get; private set; }
    public string EstadoHorario { get; private set; } = "Disponible";
    public string TenantId { get; set; } = string.Empty;

    public Usuario? Usuario { get; private set; }

    /// <summary>Ajusta la hora de fin del horario, validando que sea posterior al inicio.</summary>
    public void AjustarHoraFin(TimeSpan horaFin)
    {
        if (horaFin <= HoraEstimadaCita)
            throw new DomainException("La hora de fin debe ser posterior a la de inicio.");
        HoraEstimadaFin = horaFin;
    }
}
