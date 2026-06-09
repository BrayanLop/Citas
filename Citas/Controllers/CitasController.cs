using Citas.Application.Agendamiento;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class CitasController : ControllerBase
{
    private readonly ICitaAppService _citas;

    public CitasController(ICitaAppService citas) => _citas = citas;

    /// <summary>
    /// Lista las citas del tenant. Filtra opcionalmente por cliente o empleado
    /// (?clienteId= para "mis citas", ?empleadoId= para "mi agenda").
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CitaResponseDto>>> GetCitas(
        [FromQuery] int? clienteId = null,
        [FromQuery] int? empleadoId = null)
        => Ok(await _citas.ListarAsync(clienteId, empleadoId));

    [HttpPost]
    public async Task<ActionResult<CitaResponseDto>> CreateCita([FromBody] CreateCitaDto dto)
    {
        var cita = await _citas.AgendarAsync(dto);
        return CreatedAtAction(nameof(GetCitas), new { id = cita.IdCita }, cita);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCita(int id, [FromBody] UpdateCitaDto dto)
        => await _citas.ActualizarAsync(id, dto) ? NoContent() : NotFound("Cita no encontrada");

    [HttpPatch("{id}")]
    public async Task<IActionResult> PartialUpdateCita(int id, [FromBody] PartialUpdateCitaDto dto)
        => await _citas.CambiarEstadoAsync(id, dto) ? NoContent() : NotFound("Cita no encontrada");

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCita(int id)
        => await _citas.EliminarAsync(id) ? NoContent() : NotFound("Cita no encontrada");
}
