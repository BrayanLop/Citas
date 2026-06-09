using System.Security.Claims;
using Citas.Application.Acceso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthAppService _auth;

    public AuthController(IAuthAppService auth) => _auth = auth;

    /// <summary>
    /// Emite un JWT con el claim 'tenant' para la empresa indicada.
    /// Usado por el puente de empleado (Natural -> Citas). NO valida credenciales.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<ActionResult<TokenResponseDto>> GenerarToken([FromBody] TokenRequestDto dto)
        => Ok(await _auth.GenerarTokenAsync(dto));

    /// <summary>Registro de un cliente externo. Crea una Persona global con contraseña (PBKDF2).</summary>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<ClienteAuthResponseDto>> Register([FromBody] RegisterClienteDto dto)
        => Ok(await _auth.RegisterAsync(dto));

    /// <summary>Login global del cliente (email + contraseña contra Persona).</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ClienteAuthResponseDto>> Login([FromBody] LoginClienteDto dto)
        => Ok(await _auth.LoginAsync(dto));

    /// <summary>
    /// Tras elegir empresa, asegura un Usuario(cliente) en ese tenant ligado a la Persona global
    /// y emite un token con el claim 'tenant' para operar en esa empresa.
    /// </summary>
    [Authorize]
    [HttpPost("select-empresa")]
    public async Task<ActionResult<SelectEmpresaResponseDto>> SelectEmpresa([FromBody] SelectEmpresaDto dto)
    {
        if (!int.TryParse(User.FindFirstValue("personaId"), out var personaId))
            return Unauthorized("Token sin personaId");

        return Ok(await _auth.SelectEmpresaAsync(personaId, dto));
    }

    /// <summary>
    /// Puente Natural -> Citas: un empleado autenticado en Natural entra a una empresa/tenant.
    /// Si hay clave compartida (NaturalJwt:Key) se valida el token de Natural. Asegura el
    /// Usuario(empleado) en el tenant y emite un token de Citas en modo empresa.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("token-empleado")]
    public async Task<ActionResult<SelectEmpresaResponseDto>> TokenEmpleado([FromBody] EmpleadoTokenRequestDto dto)
        => Ok(await _auth.TokenEmpleadoAsync(dto));
}
