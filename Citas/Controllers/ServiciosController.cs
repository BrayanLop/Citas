using Citas.Application.Servicios;
using Citas.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
// Lectura: cualquier miembro del tenant. Escritura: solo personal '01'/'03'.
// (Las políticas de clase y de acción se suman, por eso van por acción.)
public class ServiciosController : ControllerBase
{
    private readonly IServicioAppService _servicios;

    public ServiciosController(IServicioAppService servicios) => _servicios = servicios;

    /// <summary>Catálogo de servicios del tenant (lectura para personal y clientes).</summary>
    [Authorize(Policy = Politicas.MiembroTenant)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServicioResponseDto>>> GetServicios()
        => Ok(await _servicios.ListarAsync());

    [Authorize(Policy = Politicas.Administrador)]
    [HttpPost]
    public async Task<ActionResult<ServicioResponseDto>> CreateServicio([FromBody] CreateServicioDto dto)
    {
        var servicio = await _servicios.CrearAsync(dto);
        return CreatedAtAction(nameof(GetServicios), new { id = servicio.IdServicio }, servicio);
    }

    [Authorize(Policy = Politicas.Administrador)]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateServicio(int id, [FromBody] UpdateServicioDto dto)
        => await _servicios.ActualizarAsync(id, dto) ? NoContent() : NotFound("Servicio no encontrado");

    [Authorize(Policy = Politicas.Administrador)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteServicio(int id)
        => await _servicios.EliminarAsync(id) ? NoContent() : NotFound("Servicio no encontrado");
}
