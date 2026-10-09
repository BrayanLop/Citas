using Citas.Domain.Agendamiento;
using Citas.Domain.Common;

namespace Citas.Models;

// La cita es la raíz del agregado de agendamiento. La idea es que nadie pueda dejarla
// en un estado inconsistente desde fuera: se crea con Programar(), cambia de estado y se
// reprograma solo por métodos que validan las reglas, y todos los setters son privados.
//
// Fechas: Inicio y Fin son instantes en UTC (DateTime con Kind=Utc). Pasarlos a la hora
// local del negocio (y validar el horario del empleado, cruces, etc.) es cosa de la capa
// de aplicación, que conoce la zona horaria configurada.
public class Cita
{
    private Cita() { }

    public int IdCita { get; private set; }
    public int IdCliente { get; private set; }
    public int IdEmpleado { get; private set; }
    public string Estado { get; private set; } = EstadoCita.Pendiente;

    /// <summary>Instante de inicio (UTC).</summary>
    public DateTime Inicio { get; private set; }

    /// <summary>Instante de fin (UTC): inicio + suma de las duraciones de los servicios.</summary>
    public DateTime Fin { get; private set; }

    public string? Observaciones { get; private set; }

    // Datos del cliente y el empleado que el mapper rellena al leer (para mostrar nombres).
    public Usuario? Cliente { get; private set; }
    public Usuario? Empleado { get; private set; }

    // Servicios de la cita (DetalleCita). Se fijan al programarla y se pueden reemplazar
    // al reprogramar; las líneas que salen quedan en DetallesRetirados para la persistencia.
    private List<DetalleCita> _detalles = [];
    public IReadOnlyList<DetalleCita> Detalles => _detalles;

    private readonly List<DetalleCita> _detallesRetirados = [];
    public IReadOnlyList<DetalleCita> DetallesRetirados => _detallesRetirados;

    /// <summary>Indica que Reprogramar reemplazó los servicios (hay líneas nuevas que insertar).</summary>
    public bool DetallesReemplazados { get; private set; }

    /// <summary>Duración total en minutos (suma de los servicios de la cita).</summary>
    public int DuracionTotal => _detalles.Sum(d => d.TiempoEstimado);

    public decimal ValorTotal => _detalles.Sum(d => d.ValorServicio);

    /// <summary>Pendiente o Confirmada: ocupa la agenda del empleado.</summary>
    public bool EstaActiva => EstadoCita.EsActivo(Estado);

    // Programa una cita nueva en estado Pendiente. Debe llevar al menos un servicio
    // agendable: la hora de fin es inicio + suma de sus TiempoEstimado.
    public static Cita Programar(
        int idCliente,
        int idEmpleado,
        DateTime inicioUtc,
        string? observaciones,
        IReadOnlyCollection<Servicio> servicios)
    {
        if (idCliente <= 0)
            throw new DomainException("La cita requiere un cliente válido.");
        ValidarEmpleado(idCliente, idEmpleado);

        var cita = new Cita
        {
            _detalles = CrearDetalles(servicios),
            IdCliente = idCliente,
            IdEmpleado = idEmpleado,
            Observaciones = observaciones,
            Estado = EstadoCita.Pendiente,
        };
        cita.FijarInicio(inicioUtc);
        return cita;
    }

    // Reconstruye una cita ya existente a partir de lo que viene de la base de datos.
    // No valida reglas: esos datos ya pasaron por Programar() en su momento.
    public static Cita Rehidratar(
        int idCita,
        int idCliente,
        int idEmpleado,
        string estado,
        DateTime inicio,
        DateTime fin,
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
            Inicio = DateTime.SpecifyKind(inicio, DateTimeKind.Utc),
            Fin = DateTime.SpecifyKind(fin, DateTimeKind.Utc),
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

    // Mueve la cita (fecha/hora y/o empleado) y, si llegan, reemplaza sus servicios
    // (nueva foto de valor y duración). Una cita atendida o cancelada ya no se toca.
    public void Reprogramar(int idEmpleado, DateTime inicioUtc, IReadOnlyCollection<Servicio>? servicios)
    {
        if (!EstaActiva)
            throw new DomainException($"Una cita '{Estado}' no se puede reprogramar.");
        ValidarEmpleado(IdCliente, idEmpleado);

        if (servicios is not null)
        {
            var nuevos = CrearDetalles(servicios);
            _detallesRetirados.AddRange(_detalles.Where(d => d.IdDetalle > 0));
            _detalles = nuevos;
            DetallesReemplazados = true;
        }

        IdEmpleado = idEmpleado;
        FijarInicio(inicioUtc);
    }

    public void ActualizarObservaciones(string? observaciones) => Observaciones = observaciones;

    // La persistencia asigna el id real una sola vez, tras el primer guardado.
    public void EstablecerIdentidad(int id)
    {
        if (IdCita == 0) IdCita = id;
    }

    private void FijarInicio(DateTime inicioUtc)
    {
        if (inicioUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("El inicio de la cita debe venir en UTC.", nameof(inicioUtc));
        Inicio = inicioUtc;
        Fin = inicioUtc.AddMinutes(DuracionTotal);
    }

    private static void ValidarEmpleado(int idCliente, int idEmpleado)
    {
        if (idEmpleado <= 0)
            throw new DomainException("La cita requiere un empleado válido.");
        if (idCliente == idEmpleado)
            throw new DomainException("El cliente y el empleado no pueden ser la misma persona.");
    }

    private static List<DetalleCita> CrearDetalles(IReadOnlyCollection<Servicio> servicios)
    {
        if (servicios.Count == 0)
            throw new DomainException("La cita debe incluir al menos un servicio (de ellos sale su duración).");

        var detalles = new List<DetalleCita>();
        foreach (var servicio in servicios)
        {
            if (!servicio.EsAgendable)
                throw new DomainException($"El servicio '{servicio.NombreServicio}' no está disponible para agendar.");
            if (detalles.Any(d => d.IdServicio == servicio.IdServicio))
                throw new DomainException($"El servicio '{servicio.NombreServicio}' está repetido en la cita.");
            detalles.Add(DetalleCita.DeServicio(servicio));
        }
        return detalles;
    }
}
