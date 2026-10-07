using Citas.Application.Agendamiento;
using Citas.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

// Las reglas por registro (el cliente solo ve/cancela sus citas, el trabajador '02' solo
// cambia el estado de las que atiende) las aplica CitaAppService con IUsuarioActual.
[ApiController]
[Route("[controller]")]
[Authorize(Policy = Politicas.MiembroTenant)]
public class CitasController : ControllerBase
{
    private readonly ICitaAppService _citas;

    public CitasController(ICitaAppService citas) => _citas = citas;

    /// <summary>
    /// Lista las citas del tenant (el cliente solo recibe las suyas). Filtra
    /// opcionalmente por cliente o empleado (?clienteId=, ?empleadoId=).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CitaResponseDto>>> GetCitas(
        [FromQuery] int? clienteId = null,
        [FromQuery] int? empleadoId = null)
        => Ok(await _citas.ListarAsync(clienteId, empleadoId));

    /// <summary>Agenda una cita: el cliente para sí mismo; el personal '01'/'03' para cualquiera.</summary>
    [Authorize(Policy = Politicas.ClienteOAdministrador)]
    [HttpPost]
    public async Task<ActionResult<CitaResponseDto>> CreateCita([FromBody] CreateCitaDto dto)
    {
        var cita = await _citas.AgendarAsync(dto);
        return CreatedAtAction(nameof(GetCitas), new { id = cita.IdCita }, cita);
    }

    /// <summary>Reprograma una cita (solo personal '01'/'03').</summary>
    [Authorize(Policy = Politicas.Administrador)]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCita(int id, [FromBody] UpdateCitaDto dto)
        => await _citas.ActualizarAsync(id, dto) ? NoContent() : NotFound("Cita no encontrada");

    /// <summary>
    /// Cambia el estado: '01'/'03' cualquiera, '02' solo las que atiende, el cliente solo
    /// puede cancelar las suyas.
    /// </summary>
    [HttpPatch("{id}")]
    public async Task<IActionResult> PartialUpdateCita(int id, [FromBody] PartialUpdateCitaDto dto)
        => await _citas.CambiarEstadoAsync(id, dto) ? NoContent() : NotFound("Cita no encontrada");

    /// <summary>Elimina una cita (solo personal '01'/'03').</summary>
    [Authorize(Policy = Politicas.Administrador)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCita(int id)
        => await _citas.EliminarAsync(id) ? NoContent() : NotFound("Cita no encontrada");
}
