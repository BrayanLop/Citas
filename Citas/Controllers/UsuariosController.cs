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
public class UsuariosController : ControllerBase
{
    private readonly IRepository<Usuario> _usuarios;
    private readonly CitasContext _db;

    public UsuariosController(IRepository<Usuario> usuarios, CitasContext db)
    {
        _usuarios = usuarios;
        _db = db;
    }

    /// <summary>
    /// Lista los usuarios del tenant actual con su rol principal.
    /// Filtra opcionalmente por nombre de rol (ej: ?rol=Empleado).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetUsuarios([FromQuery] string? rol = null)
    {
        var usuarios = await _db.Usuarios
            .Include(u => u.RolUsuarios)
                .ThenInclude(ru => ru.Rol)
            .ToListAsync();

        var response = usuarios.Select(u => new UsuarioResponseDto
        {
            Id = u.Id,
            NombreUsuario = u.NombreUsuario,
            PersonaId = u.PersonaId,
            Rol = u.RolUsuarios.Select(ru => ru.Rol!.NombreRol).FirstOrDefault(),
        });

        if (!string.IsNullOrWhiteSpace(rol))
            response = response.Where(u => string.Equals(u.Rol, rol, StringComparison.OrdinalIgnoreCase));

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<UsuarioResponseDto>> CreateUsuario([FromBody] CreateUsuarioDto dto)
    {
        var usuario = new Usuario
        {
            NombreUsuario = dto.NombreUsuario
        };

        await _usuarios.AddAsync(usuario);
        await _usuarios.SaveChangesAsync();

        var response = new UsuarioResponseDto
        {
            Id = usuario.Id,
            NombreUsuario = usuario.NombreUsuario,
            PersonaId = usuario.PersonaId,
        };

        return CreatedAtAction(nameof(GetUsuarios), new { id = usuario.Id }, response);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUsuario(int id)
    {
        var usuario = await _usuarios.GetByIdAsync(id);
        if (usuario == null)
            return NotFound("Usuario no encontrado");

        _usuarios.Remove(usuario);
        await _usuarios.SaveChangesAsync();

        return NoContent();
    }
}
