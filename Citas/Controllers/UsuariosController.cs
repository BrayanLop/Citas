using Citas.Application.Acceso;
using Citas.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
// Lectura: cualquier miembro del tenant. Escritura: solo personal '01'/'03'.
// (Las políticas de clase y de acción se suman, por eso van por acción.)
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioAppService _usuarios;

    public UsuariosController(IUsuarioAppService usuarios) => _usuarios = usuarios;

    /// <summary>
    /// Lista los usuarios del tenant con su rol principal (?rol=Empleado). El cliente solo
    /// recibe los empleados activos, para elegir con quién agendar.
    /// </summary>
    [Authorize(Policy = Politicas.MiembroTenant)]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponseDto>>> GetUsuarios([FromQuery] string? rol = null)
        => Ok(await _usuarios.ListarAsync(rol));

    [Authorize(Policy = Politicas.Administrador)]
    [HttpPost]
    public async Task<ActionResult<UsuarioResponseDto>> CreateUsuario([FromBody] CreateUsuarioDto dto)
    {
        var usuario = await _usuarios.CrearAsync(dto);
        return CreatedAtAction(nameof(GetUsuarios), new { id = usuario.Id }, usuario);
    }

    [Authorize(Policy = Politicas.Administrador)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUsuario(int id)
        => await _usuarios.EliminarAsync(id) ? NoContent() : NotFound("Usuario no encontrado");
}
