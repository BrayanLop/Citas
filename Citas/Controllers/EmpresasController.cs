using Citas.Application.Empresas;
using Citas.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
public class EmpresasController : ControllerBase
{
    private readonly IEmpresaAppService _empresas;

    public EmpresasController(IEmpresaAppService empresas) => _empresas = empresas;

    /// <summary>Empresas activas (solo Id y Nombre), para que el cliente elija dónde agendar.</summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmpresaPublicaDto>>> GetEmpresas()
        => Ok(await _empresas.ListarActivasAsync());

    /// <summary>
    /// "Activar agenda": crea o reactiva en Citas la empresa del token de Natural
    /// (Id = empresaId del token; nunca se toma del body). Solo roles '01' y '03'.
    /// </summary>
    [Authorize(Policy = Politicas.ActivarAgenda)]
    [HttpPost]
    public async Task<ActionResult<EmpresaResponseDto>> ActivarAgenda([FromBody] ActivarAgendaDto dto)
        => Ok(await _empresas.ActivarAgendaAsync(dto));
}
