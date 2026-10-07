using Citas.Application.Acceso;
using Citas.Application.Acceso.Abstractions;
using Citas.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

/// <summary>
/// Autenticación de clientes externos (cuenta propia de Citas). El personal de la
/// empresa NO pasa por aquí: llama a Citas directamente con su token de Natural.
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthAppService _auth;
    private readonly IUsuarioActual _actual;

    public AuthController(IAuthAppService auth, IUsuarioActual actual)
    {
        _auth = auth;
        _actual = actual;
    }

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
    /// Tras elegir empresa (debe existir y estar activa), asegura el Usuario del cliente
    /// en ese tenant y emite un token de cliente con el claim 'tenant'.
    /// </summary>
    [Authorize(Policy = Politicas.ClienteGlobal)]
    [HttpPost("select-empresa")]
    public async Task<ActionResult<SelectEmpresaResponseDto>> SelectEmpresa([FromBody] SelectEmpresaDto dto)
        => Ok(await _auth.SelectEmpresaAsync(_actual.PersonaId!.Value, dto));
}
