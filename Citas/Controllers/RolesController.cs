using Citas.Application.Acceso;
using Citas.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Policy = Politicas.Administrador)]
public class RolesController : ControllerBase
{
    private readonly IRolAppService _roles;

    public RolesController(IRolAppService roles) => _roles = roles;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RolResponseDto>>> GetRoles()
        => Ok(await _roles.ListarAsync());
}
