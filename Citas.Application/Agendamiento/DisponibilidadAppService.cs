using Citas.Domain.Agendamiento;
using Citas.Domain.Common;

namespace Citas.Application.Agendamiento;

/// <summary>Horas y días libres de un empleado para un conjunto de servicios.</summary>
public interface IDisponibilidadAppService
{
    Task<DisponibilidadDto> HorasLibresAsync(
        int empleadoId,
        DateOnly fecha,
        IReadOnlyCollection<int> idServicios,
        CancellationToken cancellationToken = default);

    Task<DiasDisponiblesDto> DiasConHuecoAsync(
        int empleadoId,
        DateOnly desde,
        DateOnly hasta,
        IReadOnlyCollection<int> idServicios,
        CancellationToken cancellationToken = default);
}

// Considera el horario semanal, los bloqueos, las citas activas (Pendiente/Confirmada), la
// hora actual en la zona del negocio y la anticipación mínima. Es informativa: la garantía
// la dan las validaciones al crear/reprogramar y la restricción de exclusión en la BD.
public class DisponibilidadAppService : IDisponibilidadAppService
{
    private const int DiasMaximos = 62;

    private readonly IAgendaEmpleadoRepository _agendas;
    private readonly ReglasAgenda _reglas;
    private readonly RelojAgenda _reloj;
    private readonly OpcionesAgenda _opciones;

    public DisponibilidadAppService(
        IAgendaEmpleadoRepository agendas,
        ReglasAgenda reglas,
        RelojAgenda reloj,
        OpcionesAgenda opciones)
    {
        _agendas = agendas;
        _reglas = reglas;
        _reloj = reloj;
        _opciones = opciones;
    }

    public async Task<DisponibilidadDto> HorasLibresAsync(
        int empleadoId,
        DateOnly fecha,
        IReadOnlyCollection<int> idServicios,
        CancellationToken cancellationToken = default)
    {
        var duracion = await PrepararAsync(empleadoId, idServicios, cancellationToken);
        var agenda = await _agendas.ObtenerHorarioAsync(empleadoId, cancellationToken);
        var (desdeUtc, hastaUtc) = _reloj.DiaEnUtc(fecha);
        var ocupados = await _reglas.OcupadosAsync(empleadoId, desdeUtc, hastaUtc, cancellationToken);

        return new DisponibilidadDto
        {
            IdEmpleado = empleadoId,
            Fecha = fecha,
            DuracionTotal = duracion,
            IntervaloMinutos = _opciones.IntervaloMinutos,
            ZonaHoraria = _reloj.Zona.Id,
            Horas = _reglas.HorasLibres(agenda, fecha, duracion, ocupados).Select(h => h.ToTimeSpan()).ToList(),
        };
    }

    public async Task<DiasDisponiblesDto> DiasConHuecoAsync(
        int empleadoId,
        DateOnly desde,
        DateOnly hasta,
        IReadOnlyCollection<int> idServicios,
        CancellationToken cancellationToken = default)
    {
        if (hasta < desde)
            throw new DomainException("'hasta' no puede ser anterior a 'desde'.");
        if (hasta.DayNumber - desde.DayNumber >= DiasMaximos)
            throw new DomainException($"Consulta como máximo {DiasMaximos} días.");

        var duracion = await PrepararAsync(empleadoId, idServicios, cancellationToken);
        var agenda = await _agendas.ObtenerHorarioAsync(empleadoId, cancellationToken);
        var ocupados = await _reglas.OcupadosAsync(
            empleadoId, _reloj.DiaEnUtc(desde).Desde, _reloj.DiaEnUtc(hasta).Hasta, cancellationToken);

        var dias = new List<DateOnly>();
        for (var dia = desde; dia <= hasta; dia = dia.AddDays(1))
        {
            if (_reglas.HorasLibres(agenda, dia, duracion, ocupados).Count > 0)
                dias.Add(dia);
        }

        return new DiasDisponiblesDto
        {
            IdEmpleado = empleadoId,
            Desde = desde,
            Hasta = hasta,
            DuracionTotal = duracion,
            Dias = dias,
        };
    }

    // Empleado activo (404) y duración total de los servicios pedidos (400 si no son válidos).
    private async Task<int> PrepararAsync(int empleadoId, IReadOnlyCollection<int> idServicios, CancellationToken cancellationToken)
    {
        if (await _reglas.EmpleadoActivoAsync(empleadoId, cancellationToken) is null)
            throw new NotFoundAppException("Empleado no encontrado o inactivo.");
        var servicios = await _reglas.ServiciosAsync(idServicios, cancellationToken);
        return servicios.Sum(s => s.TiempoEstimado);
    }
}
