using Citas.Application.Diagnostico;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly IHealthAppService _health;

    public HealthController(IHealthAppService health) => _health = health;

    [HttpGet]
    public async Task<ActionResult<HealthStatusDto>> Health()
        => Ok(await _health.ComprobarAsync());
}
