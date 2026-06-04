using Citas.DTOs;
using Citas.Models;
using Citas.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
public class CitasController : ControllerBase
{
    private readonly IRepository<Cita> _citas;
    private readonly IRepository<Usuario> _usuarios;

    public CitasController(IRepository<Cita> citas, IRepository<Usuario> usuarios)
    {
        _citas = citas;
        _usuarios = usuarios;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CitaResponseDto>>> GetCitas()
    {
        var citas = await _citas.GetAllAsync(
            c => c.Cliente!,
            c => c.Empleado!,
            c => c.DetalleCitas);

        var response = citas.Select(c => new CitaResponseDto
        {
            IdCita = c.IdCita,
            IdCliente = c.IdCliente,
            IdEmpleado = c.IdEmpleado,
            Estado = c.Estado,
            FechaCita = c.FechaCita,
            HoraEstimadaCita = c.HoraEstimadaCita,
            HoraEstimadaFin = c.HoraEstimadaFin,
            Observaciones = c.Observaciones
        });

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<CitaResponseDto>> CreateCita([FromBody] CreateCitaDto dto)
    {
        var clienteExists = await _usuarios.ExistsAsync(u => u.Id == dto.IdCliente);
        var empleadoExists = await _usuarios.ExistsAsync(u => u.Id == dto.IdEmpleado);

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

        await _citas.AddAsync(cita);
        await _citas.SaveChangesAsync();

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
        var cita = await _citas.GetByIdAsync(id);
        if (cita == null)
            return NotFound("Cita no encontrada");

        if (dto.Estado != null)
            cita.Estado = dto.Estado;
        if (dto.FechaCita.HasValue)
            cita.FechaCita = dto.FechaCita.Value;
        if (dto.HoraEstimadaFin.HasValue)
            cita.HoraEstimadaFin = dto.HoraEstimadaFin.Value;

        _citas.Update(cita);
        await _citas.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> PartialUpdateCita(int id, [FromBody] PartialUpdateCitaDto dto)
    {
        var cita = await _citas.GetByIdAsync(id);
        if (cita == null)
            return NotFound("Cita no encontrada");

        if (dto.Estado != null)
            cita.Estado = dto.Estado;

        _citas.Update(cita);
        await _citas.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCita(int id)
    {
        var cita = await _citas.GetByIdAsync(id);
        if (cita == null)
            return NotFound("Cita no encontrada");

        _citas.Remove(cita);
        await _citas.SaveChangesAsync();

        return NoContent();
    }
}
