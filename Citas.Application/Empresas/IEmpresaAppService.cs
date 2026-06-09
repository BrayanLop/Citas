namespace Citas.Application.Empresas;

public interface IEmpresaAppService
{
    Task<IReadOnlyList<EmpresaResponseDto>> ListarAsync(CancellationToken cancellationToken = default);

    Task<EmpresaResponseDto> CrearAsync(CreateEmpresaDto dto, CancellationToken cancellationToken = default);
}
