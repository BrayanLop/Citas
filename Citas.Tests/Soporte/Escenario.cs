using Citas.Application.Acceso.Abstractions;
using Citas.Application.Agendamiento;
using Citas.Models;

namespace Citas.Tests.Soporte;

/// <summary>
/// Negocio de ejemplo para las pruebas de aplicación, todo en memoria:
///  - "Hoy" es miércoles 2026-11-25 a las 10:00 (hora de Bogotá).
///  - Empleado 10 (personal de Natural, activo) trabaja de lunes a viernes 09:00-12:00 y 14:00-18:00.
///  - Clientes 20 y 21. Empleado inactivo 11.
///  - Servicios: 1 Corte (30 min), 2 Barba (15 min), 3 Color (90 min), 4 Retirado (no agendable).
/// </summary>
public sealed class Escenario
{
    public const int Empleado = 10;
    public const int EmpleadoInactivo = 11;
    public const int Cliente = 20;
    public const int OtroCliente = 21;

    public static readonly DateOnly Hoy = new(2026, 11, 25);      // miércoles
    public static readonly DateOnly Lunes = new(2026, 11, 30);    // primer lunes futuro
    public static readonly DateOnly Domingo = new(2026, 11, 29);

    public RelojFalso Tiempo { get; }
    public OpcionesAgenda Opciones { get; }
    public RelojAgenda Reloj { get; }
    public RepositorioUsuariosMemoria Usuarios { get; } = new();
    public RepositorioServiciosMemoria Servicios { get; } = new();
    public RepositorioCitasMemoria Citas { get; }
    public RepositorioAgendaMemoria Agenda { get; } = new();
    public UsuarioActualFalso Actual { get; } = new();
    public ReglasAgenda Reglas { get; }
    public CitaAppService CitasApp { get; }
    public DisponibilidadAppService Disponibilidad { get; }

    public Escenario(int anticipacionMinutos = 0, int intervaloMinutos = 15)
    {
        // 2026-11-25 10:00 en Bogotá (UTC-5) = 15:00 UTC.
        Tiempo = new RelojFalso(new DateTimeOffset(2026, 11, 25, 15, 0, 0, TimeSpan.Zero));
        Opciones = new OpcionesAgenda
        {
            ZonaHoraria = OpcionesAgenda.ResolverZona("America/Bogota"),
            IntervaloMinutos = intervaloMinutos,
            AnticipacionMinimaMinutos = anticipacionMinutos,
        };
        Reloj = new RelojAgenda(Opciones, Tiempo);
        Citas = new RepositorioCitasMemoria(Usuarios);

        Usuarios.Datos.Add(Usuario.Rehidratar(Empleado, "barbero", null, true, null, 501));
        Usuarios.Datos.Add(Usuario.Rehidratar(EmpleadoInactivo, "exempleado", null, false, null, 502));
        Usuarios.Datos.Add(Usuario.Rehidratar(Cliente, "ana", null, true, 900, null));
        Usuarios.Datos.Add(Usuario.Rehidratar(OtroCliente, "luis", null, true, 901, null));

        Servicios.Datos.Add(Servicio.Rehidratar(1, 101, "Corte", 20000m, 30, true, true));
        Servicios.Datos.Add(Servicio.Rehidratar(2, 102, "Barba", 12000m, 15, true, true));
        Servicios.Datos.Add(Servicio.Rehidratar(3, 103, "Color", 80000m, 90, true, true));
        Servicios.Datos.Add(Servicio.Rehidratar(4, 104, "Retirado", 5000m, 30, true, false));

        Agenda.ReemplazarHorarioAsync(AgendaEmpleado.Definir(Empleado, LunesAViernes(
            (new TimeOnly(9, 0), new TimeOnly(12, 0)),
            (new TimeOnly(14, 0), new TimeOnly(18, 0))))).GetAwaiter().GetResult();

        Reglas = new ReglasAgenda(Citas, Agenda, Usuarios, Servicios, Reloj, Opciones);
        CitasApp = new CitaAppService(Citas, Agenda, Reglas, Reloj, Actual, new UnidadTrabajoMemoria());
        Disponibilidad = new DisponibilidadAppService(Agenda, Reglas, Reloj, Opciones);
    }

    /// <summary>Quien hace la petición es el administrador del negocio.</summary>
    public Escenario ComoAdministrador()
    {
        Actual.Modo = ModoAcceso.Personal;
        Actual.RolNatural = RolesNatural.Administrador;
        Actual.UsuarioId = null;
        return this;
    }

    /// <summary>Quien hace la petición es un trabajador (rol 02) con ese usuario de Citas.</summary>
    public Escenario ComoTrabajador(int usuarioId)
    {
        Actual.Modo = ModoAcceso.Personal;
        Actual.RolNatural = RolesNatural.Trabajador;
        Actual.UsuarioId = usuarioId;
        return this;
    }

    /// <summary>Quien hace la petición es un cliente externo con ese usuario de Citas.</summary>
    public Escenario ComoCliente(int usuarioId)
    {
        Actual.Modo = ModoAcceso.Cliente;
        Actual.RolNatural = null;
        Actual.UsuarioId = usuarioId;
        return this;
    }

    public static IEnumerable<FranjaHoraria> LunesAViernes(params (TimeOnly Inicio, TimeOnly Fin)[] franjas)
    {
        foreach (var dia in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
            foreach (var (inicio, fin) in franjas)
                yield return new FranjaHoraria(dia, inicio, fin);
    }

    /// <summary>Instante UTC de una hora local de Bogotá ("HH:mm") en esa fecha.</summary>
    public DateTime Utc(DateOnly fecha, string hora) => Reloj.AUtc(fecha, TimeOnly.Parse(hora));

    public CreateCitaDto Dto(DateOnly fecha, string hora, int idCliente = Cliente, int idEmpleado = Empleado, params int[] servicios)
        => new()
        {
            IdCliente = idCliente,
            IdEmpleado = idEmpleado,
            FechaCita = fecha.ToDateTime(TimeOnly.MinValue),
            HoraEstimadaCita = TimeOnly.Parse(hora).ToTimeSpan(),
            IdServicios = (servicios.Length == 0 ? [1] : servicios).ToList(),
        };

    /// <summary>Agenda una cita directamente (como administrador) y devuelve su id.</summary>
    public async Task<int> AgendarAsync(DateOnly fecha, string hora, int idCliente = Cliente, params int[] servicios)
    {
        var previo = (Actual.Modo, Actual.RolNatural, Actual.UsuarioId);
        ComoAdministrador();
        var cita = await CitasApp.AgendarAsync(Dto(fecha, hora, idCliente, Empleado, servicios));
        (Actual.Modo, Actual.RolNatural, Actual.UsuarioId) = previo;
        return cita.IdCita;
    }
}
