using Citas.Application.Acceso.Abstractions;
using Citas.Application.Common;
using Citas.Domain.Servicios;
using Citas.Models;

namespace Citas.Application.Servicios;

public class ServicioAppService : IServicioAppService
{
    private readonly IServicioRepository _servicios;
    private readonly IUsuarioActual _actual;
    private readonly IUnitOfWork _uow;

    public ServicioAppService(IServicioRepository servicios, IUsuarioActual actual, IUnitOfWork uow)
    {
        _servicios = servicios;
        _actual = actual;
        _uow = uow;
    }

    public async Task<IReadOnlyList<ServicioResponseDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<Servicio> servicios = await _servicios.ListarAsync(cancellationToken);

        // El cliente solo ve lo que puede agendar.
        if (_actual.EsCliente)
            servicios = servicios.Where(s => s.EsAgendable);

        return servicios.Select(MapToDto).ToList();
    }

    public async Task<bool> ActualizarAsync(int idServicio, UpdateServicioDto dto, CancellationToken cancellationToken = default)
    {
        var servicio = await _servicios.ObtenerPorIdAsync(idServicio, cancellationToken);
        if (servicio is null)
            return false;

        servicio.AjustarEnCitas(dto.TiempoEstimado ?? servicio.TiempoEstimado, dto.Activo ?? servicio.Activo);
        _servicios.Actualizar(servicio);
        await _uow.GuardarCambiosAsync(cancellationToken);
        return true;
    }

    private static ServicioResponseDto MapToDto(Servicio s) => new()
    {
        IdServicio = s.IdServicio,
        ServicioNaturalId = s.ServicioNaturalId,
        NombreServicio = s.NombreServicio,
        Valor = s.Valor,
        TiempoEstimado = s.TiempoEstimado,
        Activo = s.Activo,
        DisponibleEnNatural = s.DisponibleEnNatural,
        Agendable = s.EsAgendable,
    };
}
