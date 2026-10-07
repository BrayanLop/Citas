using Citas.Application.Acceso.Abstractions;
using Citas.Application.Common;
using Citas.Domain.Common;
using Citas.Domain.Empresas;
using Citas.Models;

namespace Citas.Application.Empresas;

public class EmpresaAppService : IEmpresaAppService
{
    private readonly IEmpresaRepository _empresas;
    private readonly IUsuarioActual _actual;
    private readonly IUnitOfWork _uow;

    public EmpresaAppService(IEmpresaRepository empresas, IUsuarioActual actual, IUnitOfWork uow)
    {
        _empresas = empresas;
        _actual = actual;
        _uow = uow;
    }

    public async Task<IReadOnlyList<EmpresaPublicaDto>> ListarActivasAsync(CancellationToken cancellationToken = default)
    {
        var empresas = await _empresas.ListarActivasAsync(cancellationToken);
        return empresas.Select(e => new EmpresaPublicaDto { Id = e.Id, Nombre = e.Nombre }).ToList();
    }

    public async Task<EmpresaResponseDto> ActivarAgendaAsync(ActivarAgendaDto dto, CancellationToken cancellationToken = default)
    {
        // El tenant sale del token de Natural (empresaId); nunca del body.
        if (!_actual.EsAdministrador || string.IsNullOrWhiteSpace(_actual.Tenant))
            throw new ForbiddenAppException("Solo un administrador de Natural puede activar la agenda de su empresa.");

        var tenant = _actual.Tenant;
        var empresa = await _empresas.ObtenerPorIdAsync(tenant, cancellationToken);

        if (empresa is null)
        {
            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new DomainException("El nombre de la empresa es obligatorio para activar la agenda.");

            empresa = Empresa.Crear(tenant, dto.Nombre);
            await _empresas.AgregarAsync(empresa, cancellationToken);
        }
        else
        {
            empresa.Activar();
            if (!string.IsNullOrWhiteSpace(dto.Nombre))
                empresa.Renombrar(dto.Nombre);
            _empresas.Actualizar(empresa);
        }

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
