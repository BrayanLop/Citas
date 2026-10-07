using Citas.Application.Acceso.Abstractions;
using Citas.Application.Common;
using Citas.Domain.Acceso;
using Citas.Domain.Agendamiento;
using Citas.Domain.Common;
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
    private readonly IUsuarioActual _actual;
    private readonly IUnitOfWork _uow;

    public CitaAppService(
        ICitaRepository citas,
        IUsuarioRepository usuarios,
        IUsuarioActual actual,
        IUnitOfWork uow)
    {
        _citas = citas;
        _usuarios = usuarios;
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

        var cita = Cita.Programar(
            idCliente,
            dto.IdEmpleado,
            dto.FechaCita,
            dto.HoraEstimadaCita,
            dto.HoraEstimadaFin,
            dto.Observaciones);

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
    };
}
