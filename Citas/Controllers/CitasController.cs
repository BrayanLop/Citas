using Citas.Data;
using Citas.DTOs;
using Citas.Models;
using Citas.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class CitasController : ControllerBase
{
    private readonly IRepository<Cita> _citas;
    private readonly IRepository<Usuario> _usuarios;
    private readonly CitasContext _db;

    public CitasController(IRepository<Cita> citas, IRepository<Usuario> usuarios, CitasContext db)
    {
        _citas = citas;
        _usuarios = usuarios;
        _db = db;
    }

    /// <summary>
    /// Lista las citas del tenant. Filtra opcionalmente por cliente o empleado
    /// (?clienteId= para "mis citas", ?empleadoId= para "mi agenda").
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CitaResponseDto>>> GetCitas(
        [FromQuery] int? clienteId = null,
        [FromQuery] int? empleadoId = null)
    {
        var query = _db.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Empleado)
            .AsQueryable();

        if (clienteId.HasValue)
            query = query.Where(c => c.IdCliente == clienteId.Value);
        if (empleadoId.HasValue)
            query = query.Where(c => c.IdEmpleado == empleadoId.Value);

        var citas = await query
            .OrderByDescending(c => c.FechaCita)
            .ThenByDescending(c => c.HoraEstimadaCita)
            .ToListAsync();

        var response = citas.Select(MapToDto);
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
            Observaciones = dto.Observaciones,
            Estado = "Pendiente"
        };

        await _citas.AddAsync(cita);
        await _citas.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCitas), new { id = cita.IdCita }, MapToDto(cita));
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

    private static CitaResponseDto MapToDto(Cita c) => new()
    {
        IdCita = c.IdCita,
        IdCliente = c.IdCliente,
        IdEmpleado = c.IdEmpleado,
        Estado = c.Estado,
        FechaCita = c.FechaCita,
        HoraEstimadaCita = c.HoraEstimadaCita,
        HoraEstimadaFin = c.HoraEstimadaFin,
        Observaciones = c.Observaciones,
        NombreCliente = c.Cliente?.NombreUsuario,
        NombreEmpleado = c.Empleado?.NombreUsuario,
    };
}
