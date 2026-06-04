using Citas.DTOs;
using Citas.Models;
using Citas.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
public class ServiciosController : ControllerBase
{
    private readonly IRepository<Servicio> _servicios;

    public ServiciosController(IRepository<Servicio> servicios)
    {
        _servicios = servicios;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServicioResponseDto>>> GetServicios()
    {
        var servicios = await _servicios.GetAllAsync();

        var response = servicios.Select(s => new ServicioResponseDto
        {
            IdServicio = s.IdServicio,
            NombreServicio = s.NombreServicio,
            Valor = s.Valor,
            TiempoEstimado = s.TiempoEstimado
        });

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<ServicioResponseDto>> CreateServicio([FromBody] CreateServicioDto dto)
    {
        var servicio = new Servicio
        {
            NombreServicio = dto.NombreServicio,
            Valor = dto.Valor,
            TiempoEstimado = dto.TiempoEstimado
        };

        await _servicios.AddAsync(servicio);
        await _servicios.SaveChangesAsync();

        var response = new ServicioResponseDto
        {
            IdServicio = servicio.IdServicio,
            NombreServicio = servicio.NombreServicio,
            Valor = servicio.Valor,
            TiempoEstimado = servicio.TiempoEstimado
        };

        return CreatedAtAction(nameof(GetServicios), new { id = servicio.IdServicio }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateServicio(int id, [FromBody] UpdateServicioDto dto)
    {
        var servicio = await _servicios.GetByIdAsync(id);
        if (servicio == null)
            return NotFound("Servicio no encontrado");

        servicio.NombreServicio = dto.NombreServicio;
        servicio.Valor = dto.Valor;
        servicio.TiempoEstimado = dto.TiempoEstimado;

        _servicios.Update(servicio);
        await _servicios.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteServicio(int id)
    {
        var servicio = await _servicios.GetByIdAsync(id);
        if (servicio == null)
            return NotFound("Servicio no encontrado");

        _servicios.Remove(servicio);
        await _servicios.SaveChangesAsync();

        return NoContent();
    }
}
