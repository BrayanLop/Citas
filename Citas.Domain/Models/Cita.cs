using Citas.Domain.Agendamiento;
using Citas.Domain.Common;

namespace Citas.Models;

// La cita es la raíz del agregado de agendamiento. La idea es que nadie pueda dejarla
// en un estado inconsistente desde fuera: se crea con Programar(), cambia de estado y se
// reprograma solo por métodos que validan las reglas, y todos los setters son privados.
public class Cita
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

    // Datos del cliente y el empleado que el mapper rellena al leer (para mostrar nombres).
    public Usuario? Cliente { get; private set; }
    public Usuario? Empleado { get; private set; }

    // Servicios de la cita (DetalleCita). Se fijan al programarla.
    private List<DetalleCita> _detalles = [];
    public IReadOnlyList<DetalleCita> Detalles => _detalles;

    /// <summary>Duración total en minutos (suma de los servicios de la cita).</summary>
    public int DuracionTotal => _detalles.Sum(d => d.TiempoEstimado);

    // Programa una cita nueva en estado Pendiente. Si lleva servicios, todos deben ser
    // agendables y la hora de fin se calcula: inicio + suma de sus TiempoEstimado.
    public static Cita Programar(
        int idCliente,
        int idEmpleado,
        DateTime fechaCita,
        TimeSpan horaInicio,
        TimeSpan? horaFin,
        string? observaciones,
        IReadOnlyCollection<Servicio>? servicios = null)
    {
        if (idCliente <= 0)
            throw new DomainException("La cita requiere un cliente válido.");
        if (idEmpleado <= 0)
            throw new DomainException("La cita requiere un empleado válido.");
        if (idCliente == idEmpleado)
            throw new DomainException("El cliente y el empleado no pueden ser la misma persona.");

        var detalles = new List<DetalleCita>();
        foreach (var servicio in servicios ?? [])
        {
            if (!servicio.EsAgendable)
                throw new DomainException($"El servicio '{servicio.NombreServicio}' no está disponible para agendar.");
            if (detalles.Any(d => d.IdServicio == servicio.IdServicio))
                throw new DomainException($"El servicio '{servicio.NombreServicio}' está repetido en la cita.");
            detalles.Add(DetalleCita.DeServicio(servicio));
        }

        if (detalles.Count > 0)
        {
            horaFin = horaInicio + TimeSpan.FromMinutes(detalles.Sum(d => d.TiempoEstimado));
            if (horaFin.Value >= TimeSpan.FromDays(1))
                throw new DomainException("Con la duración de sus servicios la cita terminaría después de medianoche.");
        }
        ValidarFranja(horaInicio, horaFin);

        return new Cita
        {
            _detalles = detalles,
            IdCliente = idCliente,
            IdEmpleado = idEmpleado,
            FechaCita = fechaCita,
            HoraEstimadaCita = horaInicio,
            HoraEstimadaFin = horaFin,
            Observaciones = observaciones,
            Estado = EstadoCita.Pendiente,
        };
    }

    // Reconstruye una cita ya existente a partir de lo que viene de la base de datos.
    // No valida reglas: esos datos ya pasaron por Programar() en su momento.
    public static Cita Rehidratar(
        int idCita,
        int idCliente,
        int idEmpleado,
        string estado,
        DateTime fechaCita,
        TimeSpan horaInicio,
        TimeSpan? horaFin,
        string? observaciones,
        Usuario? cliente,
        Usuario? empleado,
        IEnumerable<DetalleCita>? detalles = null)
        => new()
        {
            _detalles = detalles?.ToList() ?? [],
            IdCita = idCita,
            IdCliente = idCliente,
            IdEmpleado = idEmpleado,
            Estado = estado,
            FechaCita = fechaCita,
            HoraEstimadaCita = horaInicio,
            HoraEstimadaFin = horaFin,
            Observaciones = observaciones,
            Cliente = cliente,
            Empleado = empleado,
        };

    public void Confirmar() => CambiarEstadoA(EstadoCita.Confirmada);
    public void Atender() => CambiarEstadoA(EstadoCita.Atendida);
    public void Cancelar() => CambiarEstadoA(EstadoCita.Cancelada);

    // Cambia el estado solo si la transición está permitida (ver EstadoCita).
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

    // Mueve la cita de fecha/hora. Una cita atendida o cancelada ya no se toca.
    public void Reprogramar(DateTime fechaCita, TimeSpan horaInicio, TimeSpan? horaFin)
    {
        if (Estado is EstadoCita.Atendida or EstadoCita.Cancelada)
            throw new DomainException($"Una cita '{Estado}' no se puede reprogramar.");
        // Con servicios, la hora de fin la marca su duración total, no la que llegue.
        if (_detalles.Count > 0)
            horaFin = horaInicio + TimeSpan.FromMinutes(DuracionTotal);
        ValidarFranja(horaInicio, horaFin);

        FechaCita = fechaCita;
        HoraEstimadaCita = horaInicio;
        HoraEstimadaFin = horaFin;
    }

    public void ActualizarObservaciones(string? observaciones) => Observaciones = observaciones;

    // La persistencia asigna el id real una sola vez, tras el primer guardado.
    public void EstablecerIdentidad(int id)
    {
        if (IdCita == 0) IdCita = id;
    }

    private static void ValidarFranja(TimeSpan horaInicio, TimeSpan? horaFin)
    {
        if (horaFin is { } fin && fin <= horaInicio)
            throw new DomainException("La hora de fin debe ser posterior a la de inicio.");
    }
}
