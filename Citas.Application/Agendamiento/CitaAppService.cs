using Citas.Application.Common;
using Citas.Domain.Agendamiento;
using Citas.Domain.Common;
using Citas.Models;

namespace Citas.Application.Agendamiento;

public class CitaAppService : ICitaAppService
{
    private readonly ICitaRepository _citas;
    private readonly IUnitOfWork _uow;

    public CitaAppService(ICitaRepository citas, IUnitOfWork uow)
    {
        _citas = citas;
        _uow = uow;
    }

    public async Task<IReadOnlyList<CitaResponseDto>> ListarAsync(
        int? clienteId = null,
        int? empleadoId = null,
        CancellationToken cancellationToken = default)
    {
        var citas = await _citas.ListarAsync(clienteId, empleadoId, cancellationToken);
        return citas.Select(MapToDto).ToList();
    }

    public async Task<CitaResponseDto> AgendarAsync(CreateCitaDto dto, CancellationToken cancellationToken = default)
    {
        if (!await _citas.ExisteUsuarioAsync(dto.IdCliente, cancellationToken) ||
            !await _citas.ExisteUsuarioAsync(dto.IdEmpleado, cancellationToken))
            throw new DomainException("Cliente o Empleado no encontrado.");

        var cita = Cita.Programar(
            dto.IdCliente,
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
