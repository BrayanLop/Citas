using Citas.Application.Agendamiento;
using Citas.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Policy = Politicas.Administrador)]
public class AgendaEmpleadoController : ControllerBase
{
    private readonly IAgendaEmpleadoAppService _agendas;

    public AgendaEmpleadoController(IAgendaEmpleadoAppService agendas) => _agendas = agendas;

    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateAgendaEmpleado(int id, [FromBody] UpdateAgendaEmpleadoDto dto)
        => await _agendas.ActualizarAsync(id, dto) ? NoContent() : NotFound("Horario no encontrado");
}
