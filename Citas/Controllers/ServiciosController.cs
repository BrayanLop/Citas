using Citas.Application.Servicios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ServiciosController : ControllerBase
{
    private readonly IServicioAppService _servicios;

    public ServiciosController(IServicioAppService servicios) => _servicios = servicios;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServicioResponseDto>>> GetServicios()
        => Ok(await _servicios.ListarAsync());

    [HttpPost]
    public async Task<ActionResult<ServicioResponseDto>> CreateServicio([FromBody] CreateServicioDto dto)
    {
        var servicio = await _servicios.CrearAsync(dto);
        return CreatedAtAction(nameof(GetServicios), new { id = servicio.IdServicio }, servicio);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateServicio(int id, [FromBody] UpdateServicioDto dto)
        => await _servicios.ActualizarAsync(id, dto) ? NoContent() : NotFound("Servicio no encontrado");

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteServicio(int id)
        => await _servicios.EliminarAsync(id) ? NoContent() : NotFound("Servicio no encontrado");
}
