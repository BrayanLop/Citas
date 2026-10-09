using Citas.Application.Acceso.Abstractions;
using Citas.Application.Common;
using Citas.Domain.Agendamiento;
using Citas.Domain.Common;
using Citas.Models;

namespace Citas.Application.Agendamiento;

// Quién puede qué (además de la política del endpoint):
//  - Leer el horario: cualquier miembro del tenant (cliente incluido), solo de empleados activos.
//  - Leer bloqueos: cualquier personal del tenant.
//  - Cambiar horario y crear/borrar bloqueos: '01'/'03' para cualquier empleado; el
//    trabajador '02' solo los suyos (organiza su propia semana).
public class AgendaEmpleadoAppService : IAgendaEmpleadoAppService
{
    private const int DiasBloqueosPorDefecto = 31;
    private const int DiasMaximosConsulta = 366;

    private readonly IAgendaEmpleadoRepository _agendas;
    private readonly ICitaRepository _citas;
    private readonly ReglasAgenda _reglas;
    private readonly RelojAgenda _reloj;
    private readonly IUsuarioActual _actual;
    private readonly IUnitOfWork _uow;

    public AgendaEmpleadoAppService(
        IAgendaEmpleadoRepository agendas,
        ICitaRepository citas,
        ReglasAgenda reglas,
        RelojAgenda reloj,
        IUsuarioActual actual,
        IUnitOfWork uow)
    {
        _agendas = agendas;
        _citas = citas;
        _reglas = reglas;
        _reloj = reloj;
        _actual = actual;
        _uow = uow;
    }

    public async Task<HorarioEmpleadoDto> ObtenerHorarioAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        var empleado = await EmpleadoAsync(usuarioId, cancellationToken);
        var agenda = await _agendas.ObtenerHorarioAsync(usuarioId, cancellationToken);
        return MapToDto(agenda, empleado.NombreUsuario);
    }

    public async Task<HorarioEmpleadoDto> ReemplazarHorarioAsync(
        int usuarioId,
        ActualizarHorarioDto dto,
        CancellationToken cancellationToken = default)
    {
        VerificarPuedeEditar(usuarioId);
        var empleado = await EmpleadoAsync(usuarioId, cancellationToken);

        var franjas = (dto.Franjas ?? []).Select(f => new FranjaHoraria(
            (DayOfWeek)f.Dia,
            AHora(f.HoraInicio, "horaInicio"),
            AHora(f.HoraFin, "horaFin")));
        var agenda = AgendaEmpleado.Definir(usuarioId, franjas);

        // Las citas ya agendadas no se tocan aunque queden fuera del nuevo horario: el
        // horario solo condiciona las citas nuevas o reprogramadas.
        // Bajo el bloqueo de la agenda para que dos reemplazos simultáneos no se mezclen.
        return await _uow.EjecutarEnTransaccionAsync(async ct =>
        {
            await _agendas.BloquearAgendaAsync(usuarioId, ct);
            await _agendas.ReemplazarHorarioAsync(agenda, ct);
            await _uow.GuardarCambiosAsync(ct);
            return MapToDto(agenda, empleado.NombreUsuario);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<BloqueoDto>> ListarBloqueosAsync(
        int usuarioId,
        DateOnly? desde,
        DateOnly? hasta,
        CancellationToken cancellationToken = default)
    {
        var inicio = desde ?? _reloj.Hoy;
        var fin = hasta ?? inicio.AddDays(DiasBloqueosPorDefecto - 1);
        if (fin < inicio)
            throw new DomainException("'hasta' no puede ser anterior a 'desde'.");
        if (fin.DayNumber - inicio.DayNumber >= DiasMaximosConsulta)
            throw new DomainException($"Consulta como máximo {DiasMaximosConsulta} días de bloqueos.");

        var bloqueos = await _agendas.ListarBloqueosAsync(
            usuarioId,
            _reloj.DiaEnUtc(inicio).Desde,
            _reloj.DiaEnUtc(fin).Hasta,
            cancellationToken);
        return bloqueos.Select(MapToDto).ToList();
    }

    public async Task<BloqueoDto> CrearBloqueoAsync(int usuarioId, CrearBloqueoDto dto, CancellationToken cancellationToken = default)
    {
        VerificarPuedeEditar(usuarioId);
        await EmpleadoAsync(usuarioId, cancellationToken);

        var bloqueo = BloqueoAgenda.Crear(usuarioId, _reloj.AUtc(dto.Inicio), _reloj.AUtc(dto.Fin), dto.Motivo);

        return await _uow.EjecutarEnTransaccionAsync(async ct =>
        {
            // Con la agenda del empleado bloqueada nadie puede colar una cita mientras tanto.
            await _agendas.BloquearAgendaAsync(usuarioId, ct);

            var afectadas = await _citas.ListarActivasDeEmpleadoAsync(usuarioId, bloqueo.Inicio, bloqueo.Fin, null, ct);
            if (afectadas.Count > 0)
                throw new ConflictException(
                    $"El bloqueo se cruza con {afectadas.Count} cita(s) activa(s) del empleado. " +
                    "Cancélalas o reprográmalas antes de bloquear ese horario.");

            await _agendas.AgregarBloqueoAsync(bloqueo, ct);
            await _uow.GuardarCambiosAsync(ct);
            return MapToDto(bloqueo);
        }, cancellationToken);
    }

    public async Task<bool> EliminarBloqueoAsync(int usuarioId, int idBloqueo, CancellationToken cancellationToken = default)
    {
        VerificarPuedeEditar(usuarioId);
        var bloqueo = await _agendas.ObtenerBloqueoAsync(usuarioId, idBloqueo, cancellationToken);
        if (bloqueo is null)
            return false;

        _agendas.EliminarBloqueo(bloqueo);
        await _uow.GuardarCambiosAsync(cancellationToken);
        return true;
    }

    private void VerificarPuedeEditar(int usuarioId)
    {
        if (_actual.EsAdministrador)
            return;
        if (_actual.EsPersonal && _actual.UsuarioId == usuarioId)
            return;
        throw new ForbiddenAppException("Solo puedes modificar tu propia agenda.");
    }

    private async Task<Usuario> EmpleadoAsync(int usuarioId, CancellationToken cancellationToken)
        => await _reglas.EmpleadoActivoAsync(usuarioId, cancellationToken)
            ?? throw new NotFoundAppException("Empleado no encontrado o inactivo.");

    private static TimeOnly AHora(TimeSpan valor, string campo)
    {
        if (valor < TimeSpan.Zero || valor >= TimeSpan.FromDays(1))
            throw new DomainException($"'{campo}' debe ser una hora del día entre 00:00:00 y 23:59:59.");
        return TimeOnly.FromTimeSpan(valor);
    }

    private HorarioEmpleadoDto MapToDto(AgendaEmpleado agenda, string? nombre) => new()
    {
        IdEmpleado = agenda.IdEmpleado,
        NombreEmpleado = nombre,
        ZonaHoraria = _reloj.Zona.Id,
        Franjas = agenda.Franjas.Select(f => new FranjaHorariaDto
        {
            Dia = (int)f.Dia,
            HoraInicio = f.Inicio.ToTimeSpan(),
            HoraFin = f.Fin.ToTimeSpan(),
        }).ToList(),
    };

    private BloqueoDto MapToDto(BloqueoAgenda b) => new()
    {
        IdBloqueo = b.IdBloqueo,
        IdEmpleado = b.IdEmpleado,
        Inicio = _reloj.ConOffset(b.Inicio),
        Fin = _reloj.ConOffset(b.Fin),
        Motivo = b.Motivo,
    };
}
