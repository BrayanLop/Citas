using Citas.Application.Sincronizacion;
using Citas.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Policy = Politicas.Administrador)]
public class SincronizacionController : ControllerBase
{
    private readonly ISincronizacionAppService _sincronizacion;

    public SincronizacionController(ISincronizacionAppService sincronizacion) => _sincronizacion = sincronizacion;

    /// <summary>
    /// Trae servicios y personal de Natural (con el token de Natural de la petición) y los
    /// sincroniza en la empresa actual. Devuelve cuántos se crearon, actualizaron y desactivaron.
    /// 403 si Natural rechaza el token; 502 si Natural no responde.
    /// </summary>
    [HttpPost("Natural")]
    public async Task<ActionResult<ResumenSincronizacionDto>> SincronizarNatural(CancellationToken cancellationToken)
        => Ok(await _sincronizacion.SincronizarConNaturalAsync(cancellationToken));
}
