using Citas.Application.Agendamiento;
using Citas.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Citas.Controllers;

// Horas libres de un empleado para unos servicios. Fechas locales del negocio (YYYY-MM-DD);
// idServicios separados por comas (idServicios=1,2) o repetidos (idServicios=1&idServicios=2).
[ApiController]
[Route("[controller]")]
[Authorize(Policy = Politicas.MiembroTenant)]
public class DisponibilidadController : ControllerBase
{
    private readonly IDisponibilidadAppService _disponibilidad;

    public DisponibilidadController(IDisponibilidadAppService disponibilidad) => _disponibilidad = disponibilidad;

    /// <summary>Horas de inicio libres ese día para la duración total de los servicios.</summary>
    [HttpGet]
    public async Task<ActionResult<DisponibilidadDto>> GetHoras(
        [FromQuery, BindRequired] int empleadoId,
        [FromQuery, BindRequired] DateOnly fecha,
        [FromQuery] string[]? idServicios)
    {
        if (!TryLeerIds(idServicios, out var ids))
            return BadRequest("idServicios debe ser una lista de números separados por comas.");
        return Ok(await _disponibilidad.HorasLibresAsync(empleadoId, fecha, ids));
    }

    /// <summary>Días entre desde y hasta (incluidos, máx. 62) con al menos una hora libre.</summary>
    [HttpGet("dias")]
    public async Task<ActionResult<DiasDisponiblesDto>> GetDias(
        [FromQuery, BindRequired] int empleadoId,
        [FromQuery, BindRequired] DateOnly desde,
        [FromQuery, BindRequired] DateOnly hasta,
        [FromQuery] string[]? idServicios)
    {
        if (!TryLeerIds(idServicios, out var ids))
            return BadRequest("idServicios debe ser una lista de números separados por comas.");
        return Ok(await _disponibilidad.DiasConHuecoAsync(empleadoId, desde, hasta, ids));
    }

    private static bool TryLeerIds(string[]? valores, out List<int> ids)
    {
        ids = [];
        foreach (var parte in (valores ?? []).SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
        {
            if (!int.TryParse(parte, out var id))
                return false;
            ids.Add(id);
        }
        return true;
    }
}
