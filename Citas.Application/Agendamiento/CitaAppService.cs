using Citas.Application.Acceso.Abstractions;
using Citas.Application.Common;
using Citas.Domain.Acceso;
using Citas.Domain.Agendamiento;
using Citas.Domain.Common;
using Citas.Domain.Servicios;
using Citas.Models;

namespace Citas.Application.Agendamiento;

// Qué puede hacer cada quien con una cita (además de la política del endpoint):
//  - Cliente: solo sus citas (IdCliente = su usuario); crea para sí mismo y solo puede cancelar.
//  - Personal administrativo ('01'/'03'): todas las citas del tenant.
//  - Trabajador ('02'): ve la agenda del tenant y cambia el estado solo de las citas que atiende.
public class CitaAppService : ICitaAppService
{
    private readonly ICitaRepository _citas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IServicioRepository _servicios;
    private readonly IUsuarioActual _actual;
    private readonly IUnitOfWork _uow;

    public CitaAppService(
        ICitaRepository citas,
        IUsuarioRepository usuarios,
        IServicioRepository servicios,
        IUsuarioActual actual,
        IUnitOfWork uow)
    {
        _citas = citas;
        _usuarios = usuarios;
        _servicios = servicios;
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

        var empleado = await _usuarios.ObtenerPorIdAsync(dto.IdEmpleado, cancellationToken);
        if (empleado is null || !empleado.EsPersonalNatural || !empleado.EstaActivo)
            throw new DomainException("Empleado no encontrado o inactivo.");

        var servicios = await ServiciosDeLaCitaAsync(dto.IdServicios, cancellationToken);

        var cita = Cita.Programar(
            idCliente,
            dto.IdEmpleado,
            dto.FechaCita,
            dto.HoraEstimadaCita,
            dto.HoraEstimadaFin,
            dto.Observaciones,
            servicios);

        await _citas.AgregarAsync(cita, cancellationToken);
        await _uow.GuardarCambiosAsync(cancellationToken);

        return MapToDto(cita);
    }

    public async Task<bool> ActualizarAsync(int idCita, UpdateCitaDto dto, CancellationToken cancellationToken = default)
    {
        var cita = await _citas.ObtenerPorIdAsync(idCita, cancellationToken);
        if (cita is null)
            return false;

        // Reprogramar conserva la hora de inicio actual (el DTO no la trae); solo cambian
        // fecha y/u hora de fin si vienen informadas.
        if (dto.FechaCita.HasValue || dto.HoraEstimadaFin.HasValue)
            cita.Reprogramar(
                dto.FechaCita ?? cita.FechaCita,
                cita.HoraEstimadaCita,
                dto.HoraEstimadaFin ?? cita.HoraEstimadaFin);

        if (dto.Estado is not null)
            cita.CambiarEstadoA(dto.Estado);

        _citas.Actualizar(cita);
        await _uow.GuardarCambiosAsync(cancellationToken);
        return true;
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

    // Servicios pedidos para la cita: deben existir en el tenant (el filtro de tenant del
    // repositorio deja fuera los de otras empresas). Que sean agendables lo valida Cita.
    private async Task<IReadOnlyList<Servicio>> ServiciosDeLaCitaAsync(
        List<int>? ids,
        CancellationToken cancellationToken)
    {
        if (ids is null || ids.Count == 0)
            return [];
        if (ids.Distinct().Count() != ids.Count)
            throw new DomainException("La cita tiene servicios repetidos.");

        var servicios = await _servicios.ObtenerPorIdsAsync(ids, cancellationToken);
        var faltantes = ids.Except(servicios.Select(s => s.IdServicio)).ToList();
        if (faltantes.Count > 0)
            throw new DomainException($"Servicio no encontrado: {string.Join(", ", faltantes)}.");

        // Mismo orden en que se pidieron.
        return ids.Select(id => servicios.First(s => s.IdServicio == id)).ToList();
    }

    private int UsuarioActualId()
        => _actual.UsuarioId ?? throw new ForbiddenAppException("El usuario no está dado de alta en esta empresa.");

    private static CitaResponseDto MapToDto(Cita c) => new()
    {
        IdCita = c.IdCita,
        IdCliente = c.IdCliente,
        IdEmpleado = c.IdEmpleado,
        Estado = c.Estado,
        FechaCita = c.FechaCita,
        HoraEstimadaCita = c.HoraEstimadaCita,
        HoraEstimadaFin = c.HoraEstimadaFin,
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
        ValorTotal = c.Detalles.Sum(d => d.ValorServicio),
    };
}
