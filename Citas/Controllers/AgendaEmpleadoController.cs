using Citas.Application.Agendamiento;
using Citas.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

// Horario semanal y bloqueos de un empleado (usuarioId = id del Usuario de Citas).
// Escribir: '01'/'03' para cualquiera; '02' solo su propia agenda (lo valida el caso de uso).
[ApiController]
[Route("[controller]")]
[Authorize(Policy = Politicas.MiembroTenant)]
public class AgendaEmpleadoController : ControllerBase
{
    private readonly IAgendaEmpleadoAppService _agendas;

    public AgendaEmpleadoController(IAgendaEmpleadoAppService agendas) => _agendas = agendas;

    /// <summary>Horario semanal de un empleado activo (cualquier miembro del tenant).</summary>
    [HttpGet("{usuarioId:int}")]
    public async Task<ActionResult<HorarioEmpleadoDto>> GetHorario(int usuarioId)
        => Ok(await _agendas.ObtenerHorarioAsync(usuarioId));

    /// <summary>Reemplaza el horario semanal completo (lista vacía = no trabaja ningún día).</summary>
    [Authorize(Policy = Politicas.Personal)]
    [HttpPut("{usuarioId:int}")]
    public async Task<ActionResult<HorarioEmpleadoDto>> PutHorario(int usuarioId, [FromBody] ActualizarHorarioDto dto)
        => Ok(await _agendas.ReemplazarHorarioAsync(usuarioId, dto));

    /// <summary>Bloqueos del empleado entre dos fechas locales (YYYY-MM-DD, ambas incluidas).</summary>
    [Authorize(Policy = Politicas.Personal)]
    [HttpGet("{usuarioId:int}/bloqueos")]
    public async Task<ActionResult<IEnumerable<BloqueoDto>>> GetBloqueos(
        int usuarioId,
        [FromQuery] DateOnly? desde = null,
        [FromQuery] DateOnly? hasta = null)
        => Ok(await _agendas.ListarBloqueosAsync(usuarioId, desde, hasta));

    /// <summary>Crea un bloqueo/ausencia. 409 si se cruza con citas activas.</summary>
    [Authorize(Policy = Politicas.Personal)]
    [HttpPost("{usuarioId:int}/bloqueos")]
    public async Task<ActionResult<BloqueoDto>> PostBloqueo(int usuarioId, [FromBody] CrearBloqueoDto dto)
    {
        var bloqueo = await _agendas.CrearBloqueoAsync(usuarioId, dto);
        return CreatedAtAction(nameof(GetBloqueos), new { usuarioId }, bloqueo);
    }

    [Authorize(Policy = Politicas.Personal)]
    [HttpDelete("{usuarioId:int}/bloqueos/{id:int}")]
    public async Task<IActionResult> DeleteBloqueo(int usuarioId, int id)
        => await _agendas.EliminarBloqueoAsync(usuarioId, id) ? NoContent() : NotFound("Bloqueo no encontrado");
}
