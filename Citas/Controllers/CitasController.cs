using Citas.Data;
using Citas.DTOs;
using Citas.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
public class CitasController : ControllerBase
{
    private readonly CitasContext _context;

    public CitasController(CitasContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CitaResponseDto>>> GetCitas()
    {
        var citas = await _context.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Empleado)
            .Include(c => c.DetalleCitas)
            .Select(c => new CitaResponseDto
            {
                IdCita = c.IdCita,
                IdCliente = c.IdCliente,
                IdEmpleado = c.IdEmpleado,
                Estado = c.Estado,
                FechaCita = c.FechaCita,
                HoraEstimadaCita = c.HoraEstimadaCita,
                HoraEstimadaFin = c.HoraEstimadaFin,
                Observaciones = c.Observaciones
            })
            .ToListAsync();

        return Ok(citas);
    }

    [HttpPost]
    public async Task<ActionResult<CitaResponseDto>> CreateCita([FromBody] CreateCitaDto dto)
    {
        var clienteExists = await _context.Usuarios.AnyAsync(u => u.Id == dto.IdCliente);
        var empleadoExists = await _context.Usuarios.AnyAsync(u => u.Id == dto.IdEmpleado);

        if (!clienteExists || !empleadoExists)
            return BadRequest("Cliente o Empleado no encontrado");

        var cita = new Cita
        {
            IdCliente = dto.IdCliente,
            IdEmpleado = dto.IdEmpleado,
            FechaCita = dto.FechaCita,
            HoraEstimadaCita = dto.HoraEstimadaCita,
            Estado = "Pendiente"
        };

        _context.Citas.Add(cita);
        await _context.SaveChangesAsync();

        var response = new CitaResponseDto
        {
            IdCita = cita.IdCita,
            IdCliente = cita.IdCliente,
            IdEmpleado = cita.IdEmpleado,
            Estado = cita.Estado,
            FechaCita = cita.FechaCita,
            HoraEstimadaCita = cita.HoraEstimadaCita,
            HoraEstimadaFin = cita.HoraEstimadaFin,
            Observaciones = cita.Observaciones
        };

        return CreatedAtAction(nameof(GetCitas), new { id = cita.IdCita }, response);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCita(int id, [FromBody] UpdateCitaDto dto)
    {
        var cita = await _context.Citas.FindAsync(id);
        if (cita == null)
            return NotFound("Cita no encontrada");

        if (dto.Estado != null)
            cita.Estado = dto.Estado;
        if (dto.FechaCita.HasValue)
            cita.FechaCita = dto.FechaCita.Value;
        if (dto.HoraEstimadaFin.HasValue)
            cita.HoraEstimadaFin = dto.HoraEstimadaFin.Value;

        _context.Citas.Update(cita);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> PartialUpdateCita(int id, [FromBody] PartialUpdateCitaDto dto)
    {
        var cita = await _context.Citas.FindAsync(id);
        if (cita == null)
            return NotFound("Cita no encontrada");

        if (dto.Estado != null)
            cita.Estado = dto.Estado;

        _context.Citas.Update(cita);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCita(int id)
    {
        var cita = await _context.Citas.FindAsync(id);
        if (cita == null)
            return NotFound("Cita no encontrada");

        _context.Citas.Remove(cita);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
