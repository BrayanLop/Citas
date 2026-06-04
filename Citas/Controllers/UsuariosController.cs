using Citas.DTOs;
using Citas.Models;
using Citas.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly IRepository<Usuario> _usuarios;

    public UsuariosController(IRepository<Usuario> usuarios)
    {
        _usuarios = usuarios;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetUsuarios()
    {
        var usuarios = await _usuarios.GetAllAsync();

        var response = usuarios.Select(u => new UsuarioResponseDto
        {
            Id = u.Id,
            NombreUsuario = u.NombreUsuario
        });

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
            NombreUsuario = usuario.NombreUsuario
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
