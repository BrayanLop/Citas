using Citas.Domain.Acceso;
using Citas.Domain.Agendamiento;
using Citas.Domain.Common;
using Citas.Domain.Servicios;
using Citas.Models;

namespace Citas.Application.Agendamiento;

/// <summary>
/// Reglas de agenda compartidas por citas y disponibilidad: qué empleado y servicios son
/// válidos, si un hueco [inicio, fin) cabe en el horario del empleado sin cruzarse con
/// bloqueos ni con otras citas activas, y qué horas de inicio quedan libres en un día.
/// La disponibilidad y la validación usan exactamente los mismos criterios.
/// </summary>
public sealed class ReglasAgenda
{
    private readonly ICitaRepository _citas;
    private readonly IAgendaEmpleadoRepository _agendas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IServicioRepository _servicios;
    private readonly RelojAgenda _reloj;
    private readonly OpcionesAgenda _opciones;

    public ReglasAgenda(
        ICitaRepository citas,
        IAgendaEmpleadoRepository agendas,
        IUsuarioRepository usuarios,
        IServicioRepository servicios,
        RelojAgenda reloj,
        OpcionesAgenda opciones)
    {
        _citas = citas;
        _agendas = agendas;
        _usuarios = usuarios;
        _servicios = servicios;
        _reloj = reloj;
        _opciones = opciones;
    }

    /// <summary>Personal de Natural activo del tenant, o null.</summary>
    public async Task<Usuario?> EmpleadoActivoAsync(int idEmpleado, CancellationToken cancellationToken)
    {
        var empleado = await _usuarios.ObtenerPorIdAsync(idEmpleado, cancellationToken);
        return empleado is { EsPersonalNatural: true, EstaActivo: true } ? empleado : null;
    }

    /// <summary>
    /// Servicios pedidos (al menos uno, sin repetir, del tenant y agendables), en el orden
    /// en que se pidieron. El filtro de tenant del repositorio deja fuera los de otras empresas.
    /// </summary>
    public async Task<IReadOnlyList<Servicio>> ServiciosAsync(IReadOnlyCollection<int>? ids, CancellationToken cancellationToken)
    {
        if (ids is null || ids.Count == 0)
            throw new DomainException("Indica al menos un servicio (idServicios): de ellos sale la duración de la cita.");
        if (ids.Distinct().Count() != ids.Count)
            throw new DomainException("La cita tiene servicios repetidos.");

        var servicios = await _servicios.ObtenerPorIdsAsync(ids, cancellationToken);
        var faltantes = ids.Except(servicios.Select(s => s.IdServicio)).ToList();
        if (faltantes.Count > 0)
            throw new DomainException($"Servicio no encontrado: {string.Join(", ", faltantes)}.");

        var noAgendable = servicios.FirstOrDefault(s => !s.EsAgendable);
        if (noAgendable is not null)
            throw new DomainException($"El servicio '{noAgendable.NombreServicio}' no está disponible para agendar.");

        return ids.Select(id => servicios.First(s => s.IdServicio == id)).ToList();
    }

    /// <summary>Primer instante en que se puede empezar una cita (ahora + anticipación mínima).</summary>
    public DateTime InicioMinimoUtc => _reloj.AhoraUtc.AddMinutes(_opciones.AnticipacionMinimaMinutos);

    /// <summary>
    /// Comprueba que el hueco sirve para una cita: no está en el pasado (400), cae completo
    /// en una franja del horario (409), no se cruza con bloqueos (409) ni con otras citas
    /// activas del empleado (409). Debe llamarse con la agenda del empleado bloqueada.
    /// </summary>
    public async Task ValidarHuecoAsync(
        int idEmpleado,
        DateTime inicioUtc,
        DateTime finUtc,
        int? excluirIdCita,
        CancellationToken cancellationToken)
    {
        if (inicioUtc < InicioMinimoUtc)
            throw new DomainException(_opciones.AnticipacionMinimaMinutos > 0
                ? $"La cita debe agendarse con al menos {_opciones.AnticipacionMinimaMinutos} minutos de anticipación."
                : "No se puede agendar una cita en el pasado.");

        var inicioLocal = _reloj.ALocal(inicioUtc);
        var finLocal = _reloj.ALocal(finUtc);
        var agenda = await _agendas.ObtenerHorarioAsync(idEmpleado, cancellationToken);
        var dia = inicioLocal.DayOfWeek;

        if (finLocal.Date != inicioLocal.Date
            || !agenda.Cubre(dia, TimeOnly.FromDateTime(inicioLocal), TimeOnly.FromDateTime(finLocal)))
        {
            var franjas = agenda.FranjasDel(dia).Select(f => $"{f.Inicio:HH\\:mm}-{f.Fin:HH\\:mm}").ToList();
            throw new ConflictException(
                $"La cita ({inicioLocal:HH\\:mm}-{finLocal:HH\\:mm}) queda fuera del horario del empleado el " +
                $"{AgendaEmpleado.NombreDia(dia)} {inicioLocal:yyyy-MM-dd}" +
                (franjas.Count == 0 ? ": ese día no trabaja." : $" (atiende {string.Join(", ", franjas)})."));
        }

        var bloqueos = await _agendas.ListarBloqueosAsync(idEmpleado, inicioUtc, finUtc, cancellationToken);
        if (bloqueos.Count > 0)
        {
            var b = bloqueos[0];
            throw new ConflictException(
                $"El empleado no está disponible de {_reloj.ALocal(b.Inicio):yyyy-MM-dd HH\\:mm} a {_reloj.ALocal(b.Fin):yyyy-MM-dd HH\\:mm}" +
                (b.Motivo is null ? "." : $" ({b.Motivo})."));
        }

        var cruces = await _citas.ListarActivasDeEmpleadoAsync(idEmpleado, inicioUtc, finUtc, excluirIdCita, cancellationToken);
        if (cruces.Count > 0)
        {
            var c = cruces[0];
            throw new ConflictException(
                $"El empleado ya tiene otra cita de {_reloj.ALocal(c.Inicio):HH\\:mm} a {_reloj.ALocal(c.Fin):HH\\:mm} ese día.");
        }
    }

    /// <summary>
    /// Horas de inicio (hora local) libres en un día para una cita de <paramref name="duracionMinutos"/>,
    /// recorriendo cada franja del horario desde su inicio cada IntervaloMinutos. <paramref name="ocupados"/>
    /// son los tramos UTC de bloqueos y citas activas (pueden incluir otros días; se ignoran).
    /// </summary>
    public IReadOnlyList<TimeOnly> HorasLibres(
        AgendaEmpleado agenda,
        DateOnly fecha,
        int duracionMinutos,
        IReadOnlyCollection<(DateTime Inicio, DateTime Fin)> ocupados)
    {
        var libres = new List<TimeOnly>();
        if (fecha < _reloj.Hoy || duracionMinutos <= 0)
            return libres;

        var minimo = InicioMinimoUtc;
        var duracion = TimeSpan.FromMinutes(duracionMinutos);
        var paso = TimeSpan.FromMinutes(Math.Max(1, _opciones.IntervaloMinutos));

        foreach (var franja in agenda.FranjasDel(fecha.DayOfWeek))
        {
            var finFranja = franja.Fin.ToTimeSpan();
            for (var hora = franja.Inicio.ToTimeSpan(); hora + duracion <= finFranja; hora += paso)
            {
                var local = fecha.ToDateTime(TimeOnly.FromTimeSpan(hora));
                if (_reloj.Zona.IsInvalidTime(local))
                    continue;

                var inicio = _reloj.AUtc(local);
                var fin = inicio + duracion;
                if (inicio < minimo)
                    continue;
                if (ocupados.Any(o => o.Inicio < fin && inicio < o.Fin))
                    continue;
                // Mismo criterio que ValidarHuecoAsync (cambios de hora incluidos).
                var finLocal = _reloj.ALocal(fin);
                if (finLocal.Date != local.Date || !franja.Contiene(TimeOnly.FromDateTime(local), TimeOnly.FromDateTime(finLocal)))
                    continue;

                libres.Add(TimeOnly.FromTimeSpan(hora));
            }
        }

        return libres.Order().ToList();
    }

    /// <summary>Tramos ocupados (bloqueos + citas activas) del empleado en [desdeUtc, hastaUtc).</summary>
    public async Task<IReadOnlyCollection<(DateTime Inicio, DateTime Fin)>> OcupadosAsync(
        int idEmpleado,
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken)
    {
        var bloqueos = await _agendas.ListarBloqueosAsync(idEmpleado, desdeUtc, hastaUtc, cancellationToken);
        var citas = await _citas.ListarActivasDeEmpleadoAsync(idEmpleado, desdeUtc, hastaUtc, null, cancellationToken);
        return bloqueos.Select(b => (b.Inicio, b.Fin))
            .Concat(citas.Select(c => (c.Inicio, c.Fin)))
            .ToList();
    }
}
