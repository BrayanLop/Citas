namespace Citas.Application.Empresas;

public interface IEmpresaAppService
{
    /// <summary>Empresas activas (Id y Nombre), para que el cliente elija dónde agendar.</summary>
    Task<IReadOnlyList<EmpresaPublicaDto>> ListarActivasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea o reactiva la empresa de Citas cuyo Id es el tenant del usuario actual
    /// (empresaId de Natural).
    /// </summary>
    Task<EmpresaResponseDto> ActivarAgendaAsync(ActivarAgendaDto dto, CancellationToken cancellationToken = default);
}
