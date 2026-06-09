using Citas.Application.Common;
using Citas.Domain.Servicios;
using Citas.Models;

namespace Citas.Application.Servicios;

public class ServicioAppService : IServicioAppService
{
    private readonly IServicioRepository _servicios;
    private readonly IUnitOfWork _uow;

    public ServicioAppService(IServicioRepository servicios, IUnitOfWork uow)
    {
        _servicios = servicios;
        _uow = uow;
    }

    public async Task<IReadOnlyList<ServicioResponseDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var servicios = await _servicios.ListarAsync(cancellationToken);
        return servicios.Select(MapToDto).ToList();
    }

    public async Task<ServicioResponseDto> CrearAsync(CreateServicioDto dto, CancellationToken cancellationToken = default)
    {
        var servicio = Servicio.Crear(dto.NombreServicio, dto.Valor, dto.TiempoEstimado);
        await _servicios.AgregarAsync(servicio, cancellationToken);
        await _uow.GuardarCambiosAsync(cancellationToken);
        return MapToDto(servicio);
    }

    public async Task<bool> ActualizarAsync(int idServicio, UpdateServicioDto dto, CancellationToken cancellationToken = default)
    {
        var servicio = await _servicios.ObtenerPorIdAsync(idServicio, cancellationToken);
        if (servicio is null)
            return false;

        servicio.Actualizar(dto.NombreServicio, dto.Valor, dto.TiempoEstimado);
        await _uow.GuardarCambiosAsync(cancellationToken);
        return true;
    }

    public async Task<bool> EliminarAsync(int idServicio, CancellationToken cancellationToken = default)
    {
        var servicio = await _servicios.ObtenerPorIdAsync(idServicio, cancellationToken);
        if (servicio is null)
            return false;

        _servicios.Eliminar(servicio);
        await _uow.GuardarCambiosAsync(cancellationToken);
        return true;
    }

    private static ServicioResponseDto MapToDto(Servicio s) => new()
    {
        IdServicio = s.IdServicio,
        NombreServicio = s.NombreServicio,
        Valor = s.Valor,
        TiempoEstimado = s.TiempoEstimado,
    };
}
