using Citas.Application.Common;
using Citas.Domain.Agendamiento;

namespace Citas.Application.Agendamiento;

public class AgendaEmpleadoAppService : IAgendaEmpleadoAppService
{
    private readonly IAgendaEmpleadoRepository _agendas;
    private readonly IUnitOfWork _uow;

    public AgendaEmpleadoAppService(IAgendaEmpleadoRepository agendas, IUnitOfWork uow)
    {
        _agendas = agendas;
        _uow = uow;
    }

    public async Task<bool> ActualizarAsync(int idHorario, UpdateAgendaEmpleadoDto dto, CancellationToken cancellationToken = default)
    {
        var agenda = await _agendas.ObtenerPorIdAsync(idHorario, cancellationToken);
        if (agenda is null)
            return false;

        if (dto.HoraEstimadaFin.HasValue)
            agenda.AjustarHoraFin(dto.HoraEstimadaFin.Value);

        await _uow.GuardarCambiosAsync(cancellationToken);
        return true;
    }
}
