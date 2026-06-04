using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Citas.DTOs;
using Citas.Models;
using Citas.Multitenancy;
using Citas.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Citas.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IRepository<Empresa> _empresas;
    private readonly IConfiguration _config;

    public AuthController(IRepository<Empresa> empresas, IConfiguration config)
    {
        _empresas = empresas;
        _config = config;
    }

    /// <summary>
    /// Emite un JWT con el claim 'tenant' para la empresa indicada.
    /// NOTA: endpoint de demostración: NO valida credenciales. Reemplazar por
    /// autenticación real (login con contraseña / IdP) antes de producción.
    /// </summary>
    [HttpPost("token")]
    public async Task<ActionResult<TokenResponseDto>> GenerarToken([FromBody] TokenRequestDto dto)
    {
        var empresa = await _empresas.GetByIdAsync(dto.Tenant);
        if (empresa == null || !empresa.Activo)
            return BadRequest("Empresa/tenant no existe o está inactiva");

        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expira = DateTime.UtcNow.AddHours(8);

        var claims = new[]
        {
            new Claim(TenantClaimTypes.Tenant, empresa.Id),
            new Claim(ClaimTypes.Name, string.IsNullOrWhiteSpace(dto.Usuario) ? "demo" : dto.Usuario)
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expira,
            signingCredentials: credentials);

        return Ok(new TokenResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Tenant = empresa.Id,
            ExpiraUtc = expira
        });
    }
}
