using Citas.Data;
using Citas.DTOs;
using Citas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
public class ServiciosController : ControllerBase
{
    private readonly CitasContext _context;

    public ServiciosController(CitasContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ServicioResponseDto>>> GetServicios()
    {
        var servicios = await _context.Servicios
            .Select(s => new ServicioResponseDto
            {
                IdServicio = s.IdServicio,
                NombreServicio = s.NombreServicio,
                Valor = s.Valor,
                TiempoEstimado = s.TiempoEstimado
            })
            .ToListAsync();

        return Ok(servicios);
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

        _context.Servicios.Add(servicio);
        await _context.SaveChangesAsync();

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
        var servicio = await _context.Servicios.FindAsync(id);
        if (servicio == null)
            return NotFound("Servicio no encontrado");

        servicio.NombreServicio = dto.NombreServicio;
        servicio.Valor = dto.Valor;
        servicio.TiempoEstimado = dto.TiempoEstimado;

        _context.Servicios.Update(servicio);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteServicio(int id)
    {
        var servicio = await _context.Servicios.FindAsync(id);
        if (servicio == null)
            return NotFound("Servicio no encontrado");

        _context.Servicios.Remove(servicio);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
