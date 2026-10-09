using Citas.Application.Servicios;
using Citas.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
// Los servicios se crean y se borran en Natural y llegan con POST /Sincronizacion/Natural.
// Lectura: cualquier miembro del tenant. Ajuste (duración/disponibilidad): personal '01'/'03'.
// (Las políticas de clase y de acción se suman, por eso van por acción.)
public class ServiciosController : ControllerBase
{
    private readonly IServicioAppService _servicios;

    public ServiciosController(IServicioAppService servicios) => _servicios = servicios;

    /// <summary>Catálogo del tenant: el cliente ve solo los agendables; el personal, todos.</summary>
    [Authorize(Policy = Politicas.MiembroTenant)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServicioResponseDto>>> GetServicios()
        => Ok(await _servicios.ListarAsync());

    /// <summary>Ajusta la duración (TiempoEstimado) y/o la disponibilidad para agendar (Activo).</summary>
    [Authorize(Policy = Politicas.Administrador)]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateServicio(int id, [FromBody] UpdateServicioDto dto)
        => await _servicios.ActualizarAsync(id, dto) ? NoContent() : NotFound("Servicio no encontrado");
}
