using Citas.DTOs;
using Citas.Models;
using Citas.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class AgendaEmpleadoController : ControllerBase
{
    private readonly IRepository<AgendaEmpleado> _agendas;

    public AgendaEmpleadoController(IRepository<AgendaEmpleado> agendas)
    {
        _agendas = agendas;
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateAgendaEmpleado(int id, [FromBody] UpdateAgendaEmpleadoDto dto)
    {
        var agenda = await _agendas.GetByIdAsync(id);
        if (agenda == null)
            return NotFound("Horario no encontrado");

        if (dto.HoraEstimadaFin.HasValue)
            agenda.HoraEstimadaFin = dto.HoraEstimadaFin.Value;

        _agendas.Update(agenda);
        await _agendas.SaveChangesAsync();

        return NoContent();
    }
}
