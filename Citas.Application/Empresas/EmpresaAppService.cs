using Citas.Application.Common;
using Citas.Domain.Common;
using Citas.Domain.Empresas;
using Citas.Models;

namespace Citas.Application.Empresas;

public class EmpresaAppService : IEmpresaAppService
{
    private readonly IEmpresaRepository _empresas;
    private readonly IUnitOfWork _uow;

    public EmpresaAppService(IEmpresaRepository empresas, IUnitOfWork uow)
    {
        _empresas = empresas;
        _uow = uow;
    }

    public async Task<IReadOnlyList<EmpresaResponseDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var empresas = await _empresas.ListarAsync(cancellationToken);
        return empresas.Select(MapToDto).ToList();
    }

    public async Task<EmpresaResponseDto> CrearAsync(CreateEmpresaDto dto, CancellationToken cancellationToken = default)
    {
        var id = (dto.Id ?? string.Empty).Trim();
        if (await _empresas.ExisteAsync(id, cancellationToken))
            throw new ConflictException($"Ya existe una empresa con el código '{id}'.");

        var empresa = Empresa.Crear(dto.Id, dto.Nombre);
        await _empresas.AgregarAsync(empresa, cancellationToken);
        await _uow.GuardarCambiosAsync(cancellationToken);

        return MapToDto(empresa);
    }

    private static EmpresaResponseDto MapToDto(Empresa e) => new()
    {
        Id = e.Id,
        Nombre = e.Nombre,
        Activo = e.Activo,
    };
}
