using Citas.Data;
using Citas.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
public class AgendaEmpleadoController : ControllerBase
{
    private readonly CitasContext _context;

    public AgendaEmpleadoController(CitasContext context)
    {
        _context = context;
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateAgendaEmpleado(int id, [FromBody] UpdateAgendaEmpleadoDto dto)
    {
        var agenda = await _context.AgendaEmpleados.FindAsync(id);
        if (agenda == null)
            return NotFound("Horario no encontrado");

        if (dto.HoraEstimadaFin.HasValue)
            agenda.HoraEstimadaFin = dto.HoraEstimadaFin.Value;

        _context.AgendaEmpleados.Update(agenda);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
