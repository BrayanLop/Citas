using Citas.Domain.Agendamiento;
using Citas.Domain.Common;

namespace Citas.Models;

/// <summary>
/// Raíz de agregado del contexto de Agendamiento. Encapsula las reglas de una cita:
/// solo se crea a través de <see cref="Programar"/> y solo cambia de estado o se
/// reprograma mediante métodos que validan las invariantes. Los setters son privados
/// para que ninguna capa externa pueda dejar la cita en un estado inválido.
/// </summary>
public class Cita : ITenantEntity
{
    private Cita() { }

    public int IdCita { get; private set; }
    public int IdCliente { get; private set; }
    public int IdEmpleado { get; private set; }
    public string Estado { get; private set; } = EstadoCita.Pendiente;
    public DateTime FechaCita { get; private set; }
    public TimeSpan HoraEstimadaCita { get; private set; }
    public TimeSpan? HoraEstimadaFin { get; private set; }
    public string? Observaciones { get; private set; }

    public string TenantId { get; set; } = string.Empty;

    public Usuario? Cliente { get; private set; }
    public Usuario? Empleado { get; private set; }
    public ICollection<DetalleCita> DetalleCitas { get; private set; } = [];

    /// <summary>
    /// Programa una nueva cita en estado Pendiente, validando las invariantes básicas.
    /// </summary>
    public static Cita Programar(
        int idCliente,
        int idEmpleado,
        DateTime fechaCita,
        TimeSpan horaInicio,
        TimeSpan? horaFin,
        string? observaciones)
    {
        if (idCliente <= 0)
            throw new DomainException("La cita requiere un cliente válido.");
        if (idEmpleado <= 0)
            throw new DomainException("La cita requiere un empleado válido.");
        if (idCliente == idEmpleado)
            throw new DomainException("El cliente y el empleado no pueden ser la misma persona.");
        ValidarFranja(horaInicio, horaFin);

        return new Cita
        {
            IdCliente = idCliente,
            IdEmpleado = idEmpleado,
            FechaCita = fechaCita,
            HoraEstimadaCita = horaInicio,
            HoraEstimadaFin = horaFin,
            Observaciones = observaciones,
            Estado = EstadoCita.Pendiente,
        };
    }

    public void Confirmar() => CambiarEstadoA(EstadoCita.Confirmada);
    public void Atender() => CambiarEstadoA(EstadoCita.Atendida);
    public void Cancelar() => CambiarEstadoA(EstadoCita.Cancelada);

    /// <summary>
    /// Cambia el estado validando que la transición sea permitida.
    /// </summary>
    public void CambiarEstadoA(string nuevoEstado)
    {
        if (!EstadoCita.EsValido(nuevoEstado))
            throw new DomainException($"Estado '{nuevoEstado}' no es válido.");
        if (Estado == nuevoEstado)
            return;
        if (!EstadoCita.PuedeTransicionar(Estado, nuevoEstado))
            throw new DomainException($"No se puede pasar de '{Estado}' a '{nuevoEstado}'.");

        Estado = nuevoEstado;
    }

    /// <summary>
    /// Reprograma la cita; no se permite si ya fue atendida o cancelada.
    /// </summary>
    public void Reprogramar(DateTime fechaCita, TimeSpan horaInicio, TimeSpan? horaFin)
    {
        if (Estado is EstadoCita.Atendida or EstadoCita.Cancelada)
            throw new DomainException($"Una cita '{Estado}' no se puede reprogramar.");
        ValidarFranja(horaInicio, horaFin);

        FechaCita = fechaCita;
        HoraEstimadaCita = horaInicio;
        HoraEstimadaFin = horaFin;
    }

    public void ActualizarObservaciones(string? observaciones) => Observaciones = observaciones;

    private static void ValidarFranja(TimeSpan horaInicio, TimeSpan? horaFin)
    {
        if (horaFin is { } fin && fin <= horaInicio)
            throw new DomainException("La hora de fin debe ser posterior a la de inicio.");
    }
}
