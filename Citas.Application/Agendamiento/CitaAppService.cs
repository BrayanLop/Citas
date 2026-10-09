using Citas.Application.Acceso.Abstractions;
using Citas.Application.Common;
using Citas.Domain.Agendamiento;
using Citas.Domain.Common;
using Citas.Models;

namespace Citas.Application.Agendamiento;

// Qué puede hacer cada quien con una cita (además de la política del endpoint):
//  - Cliente: solo sus citas (IdCliente = su usuario); crea para sí mismo, reprograma las
//    suyas mientras estén Pendientes y solo puede cancelar.
//  - Personal administrativo ('01'/'03'): todas las citas del tenant.
//  - Trabajador ('02'): ve la agenda del tenant y cambia el estado solo de las citas que
//    atiende; no crea ni reprograma (lo hace administración o el propio cliente).
//
// Crear y reprogramar validan el hueco (ReglasAgenda) con la agenda del empleado
// bloqueada dentro de una transacción; la restricción de exclusión de la BD es la última
// barrera contra cruces (su violación llega aquí como ConflictException, 409).
public class CitaAppService : ICitaAppService
{
    private readonly ICitaRepository _citas;
    private readonly IAgendaEmpleadoRepository _agendas;
    private readonly ReglasAgenda _reglas;
    private readonly RelojAgenda _reloj;
    private readonly IUsuarioActual _actual;
    private readonly IUnitOfWork _uow;

    public CitaAppService(
        ICitaRepository citas,
        IAgendaEmpleadoRepository agendas,
        ReglasAgenda reglas,
        RelojAgenda reloj,
        IUsuarioActual actual,
        IUnitOfWork uow)
    {
        _citas = citas;
        _agendas = agendas;
        _reglas = reglas;
        _reloj = reloj;
        _actual = actual;
        _uow = uow;
    }

    public async Task<IReadOnlyList<CitaResponseDto>> ListarAsync(
        int? clienteId = null,
        int? empleadoId = null,
        CancellationToken cancellationToken = default)
    {
        // El cliente solo ve sus citas, pida lo que pida en el filtro.
        if (_actual.EsCliente)
            clienteId = UsuarioActualId();

        var citas = await _citas.ListarAsync(clienteId, empleadoId, cancellationToken);
        return citas.Select(MapToDto).ToList();
    }

    public async Task<CitaResponseDto> AgendarAsync(CreateCitaDto dto, CancellationToken cancellationToken = default)
    {
        var idCliente = dto.IdCliente;
        if (_actual.EsCliente)
        {
            var propio = UsuarioActualId();
            if (idCliente != 0 && idCliente != propio)
                throw new ForbiddenAppException("Solo puedes agendar citas para ti.");
            idCliente = propio;
        }

        if (!await _citas.ExisteUsuarioAsync(idCliente, cancellationToken))
            throw new DomainException("Cliente no encontrado.");
        await EmpleadoActivoAsync(dto.IdEmpleado, cancellationToken);

        var servicios = await _reglas.ServiciosAsync(dto.IdServicios, cancellationToken);
        var inicio = InicioUtc(DateOnly.FromDateTime(dto.FechaCita), dto.HoraEstimadaCita);
        var cita = Cita.Programar(idCliente, dto.IdEmpleado, inicio, dto.Observaciones, servicios);

        await _uow.EjecutarEnTransaccionAsync(async ct =>
        {
            await _agendas.BloquearAgendaAsync(cita.IdEmpleado, ct);
            await _reglas.ValidarHuecoAsync(cita.IdEmpleado, cita.Inicio, cita.Fin, null, ct);
            await _citas.AgregarAsync(cita, ct);
            return await _uow.GuardarCambiosAsync(ct);
        }, cancellationToken);

        return MapToDto(await _citas.ObtenerPorIdAsync(cita.IdCita, cancellationToken) ?? cita);
    }

    public async Task<CitaResponseDto?> ReprogramarAsync(int idCita, UpdateCitaDto dto, CancellationToken cancellationToken = default)
    {
        var cita = await _citas.ObtenerPorIdAsync(idCita, cancellationToken);
        if (cita is null)
            return null;

        if (_actual.EsCliente)
        {
            if (cita.IdCliente != UsuarioActualId())
                throw new ForbiddenAppException("La cita no es tuya.");
            if (cita.Estado != EstadoCita.Pendiente)
                throw new ForbiddenAppException("Solo puedes reprogramar tus citas mientras estén Pendientes.");
        }
        else if (!_actual.EsAdministrador)
        {
            throw new ForbiddenAppException("Solo el personal administrativo o el cliente pueden reprogramar citas.");
        }

        var mueveHueco = dto.FechaCita.HasValue || dto.HoraEstimadaCita.HasValue
            || dto.IdEmpleado.HasValue || dto.IdServicios is not null;

        if (mueveHueco)
        {
            var actualLocal = _reloj.ALocal(cita.Inicio);
            var fecha = dto.FechaCita.HasValue ? DateOnly.FromDateTime(dto.FechaCita.Value) : DateOnly.FromDateTime(actualLocal);
            var hora = dto.HoraEstimadaCita ?? actualLocal.TimeOfDay;
            var idEmpleado = dto.IdEmpleado ?? cita.IdEmpleado;

            await EmpleadoActivoAsync(idEmpleado, cancellationToken);
            var servicios = dto.IdServicios is null ? null : await _reglas.ServiciosAsync(dto.IdServicios, cancellationToken);
            cita.Reprogramar(idEmpleado, InicioUtc(fecha, hora), servicios);
        }

        if (dto.Observaciones is not null)
            cita.ActualizarObservaciones(dto.Observaciones);

        await _uow.EjecutarEnTransaccionAsync(async ct =>
        {
            if (mueveHueco)
            {
                await _agendas.BloquearAgendaAsync(cita.IdEmpleado, ct);
                await _reglas.ValidarHuecoAsync(cita.IdEmpleado, cita.Inicio, cita.Fin, cita.IdCita, ct);
            }
            _citas.Actualizar(cita);
            return await _uow.GuardarCambiosAsync(ct);
        }, cancellationToken);

        return MapToDto(await _citas.ObtenerPorIdAsync(idCita, cancellationToken) ?? cita);
    }

    public async Task<bool> CambiarEstadoAsync(int idCita, PartialUpdateCitaDto dto, CancellationToken cancellationToken = default)
    {
        var cita = await _citas.ObtenerPorIdAsync(idCita, cancellationToken);
        if (cita is null)
            return false;

        if (_actual.EsCliente)
        {
            if (cita.IdCliente != UsuarioActualId())
                throw new ForbiddenAppException("La cita no es tuya.");
            if (dto.Estado is not null && dto.Estado != EstadoCita.Cancelada)
                throw new ForbiddenAppException("Como cliente solo puedes cancelar tus citas.");
        }
        else if (!_actual.EsAdministrador && cita.IdEmpleado != UsuarioActualId())
        {
            throw new ForbiddenAppException("Solo puedes cambiar el estado de las citas que atiendes.");
        }

        // Las transiciones solo van de activa a activa (Pendiente -> Confirmada) o de activa a
        // terminal: cambiar el estado nunca crea un cruce nuevo.
        if (dto.Estado is not null)
            cita.CambiarEstadoA(dto.Estado);

        _citas.Actualizar(cita);
        await _uow.GuardarCambiosAsync(cancellationToken);
        return true;
    }

    public async Task<bool> EliminarAsync(int idCita, CancellationToken cancellationToken = default)
    {
        var cita = await _citas.ObtenerPorIdAsync(idCita, cancellationToken);
        if (cita is null)
            return false;

        _citas.Eliminar(cita);
        await _uow.GuardarCambiosAsync(cancellationToken);
        return true;
    }

    private async Task EmpleadoActivoAsync(int idEmpleado, CancellationToken cancellationToken)
    {
        if (await _reglas.EmpleadoActivoAsync(idEmpleado, cancellationToken) is null)
            throw new DomainException("Empleado no encontrado o inactivo.");
    }

    // Fecha y hora locales del negocio -> instante UTC.
    private DateTime InicioUtc(DateOnly fecha, TimeSpan hora)
    {
        if (hora < TimeSpan.Zero || hora >= TimeSpan.FromDays(1))
            throw new DomainException("'horaEstimadaCita' debe ser una hora del día (HH:mm:ss).");
        return _reloj.AUtc(fecha, TimeOnly.FromTimeSpan(hora));
    }

    private int UsuarioActualId()
        => _actual.UsuarioId ?? throw new ForbiddenAppException("El usuario no está dado de alta en esta empresa.");

    private CitaResponseDto MapToDto(Cita c)
    {
        var inicioLocal = _reloj.ALocal(c.Inicio);
        var finLocal = _reloj.ALocal(c.Fin);
        return new CitaResponseDto
        {
            IdCita = c.IdCita,
            IdCliente = c.IdCliente,
            IdEmpleado = c.IdEmpleado,
            Estado = c.Estado,
            FechaCita = inicioLocal.Date,
            HoraEstimadaCita = inicioLocal.TimeOfDay,
            HoraEstimadaFin = finLocal.TimeOfDay,
            Inicio = _reloj.ConOffset(c.Inicio),
            Fin = _reloj.ConOffset(c.Fin),
            Observaciones = c.Observaciones,
            NombreCliente = c.Cliente?.NombreUsuario,
            NombreEmpleado = c.Empleado?.NombreUsuario,
            Servicios = c.Detalles.Select(d => new DetalleCitaDto
            {
                IdServicio = d.IdServicio,
                NombreServicio = d.NombreServicio,
                ValorServicio = d.ValorServicio,
                TiempoEstimado = d.TiempoEstimado,
            }).ToList(),
            DuracionTotal = c.DuracionTotal,
            ValorTotal = c.ValorTotal,
        };
    }
}
