using Citas.Application.Acceso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioAppService _usuarios;

    public UsuariosController(IUsuarioAppService usuarios) => _usuarios = usuarios;

    /// <summary>
    /// Lista los usuarios del tenant actual con su rol principal.
    /// Filtra opcionalmente por nombre de rol (ej: ?rol=Empleado).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetUsuarios([FromQuery] string? rol = null)
        => Ok(await _usuarios.ListarAsync(rol));

    [HttpPost]
    public async Task<ActionResult<UsuarioResponseDto>> CreateUsuario([FromBody] CreateUsuarioDto dto)
    {
        var usuario = await _usuarios.CrearAsync(dto);
        return CreatedAtAction(nameof(GetUsuarios), new { id = usuario.Id }, usuario);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUsuario(int id)
        => await _usuarios.EliminarAsync(id) ? NoContent() : NotFound("Usuario no encontrado");
}
