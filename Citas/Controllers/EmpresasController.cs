using Citas.Application.Empresas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
// Administración de tenants. En producción debería restringirse a un rol de super-admin.
[AllowAnonymous]
public class EmpresasController : ControllerBase
{
    private readonly IEmpresaAppService _empresas;

    public EmpresasController(IEmpresaAppService empresas) => _empresas = empresas;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmpresaResponseDto>>> GetEmpresas()
        => Ok(await _empresas.ListarAsync());

    [HttpPost]
    public async Task<ActionResult<EmpresaResponseDto>> CreateEmpresa([FromBody] CreateEmpresaDto dto)
    {
        var empresa = await _empresas.CrearAsync(dto);
        return CreatedAtAction(nameof(GetEmpresas), new { id = empresa.Id }, empresa);
    }
}
