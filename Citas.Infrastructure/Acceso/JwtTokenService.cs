using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Citas.Application.Acceso.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Citas.Infrastructure.Acceso;

/// <summary>
/// Adaptador del puerto <see cref="ITokenService"/> con JWT firmado (HMAC-SHA256) con
/// la configuración "Jwt" de Citas. Solo emite tokens de cliente (mode = cliente).
/// </summary>
public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _config;

    public JwtTokenService(IConfiguration config) => _config = config;

    public (string Token, DateTime ExpiraUtc) Generar(TokenClaims claims, int horas = 8)
    {
        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expira = DateTime.UtcNow.AddHours(horas);

        // Al pasar los claims al constructor se escriben con su tipo tal cual (cortos).
        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: ConstruirClaims(claims),
            expires: expira,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }

    private static IEnumerable<Claim> ConstruirClaims(TokenClaims c)
    {
        var claims = new List<Claim> { new(CitasClaimTypes.Modo, CitasClaimTypes.ModoCliente) };
        if (!string.IsNullOrWhiteSpace(c.Tenant))
            claims.Add(new Claim(CitasClaimTypes.Tenant, c.Tenant));
        if (c.PersonaId is { } personaId)
            claims.Add(new Claim(CitasClaimTypes.PersonaId, personaId.ToString()));
        if (c.UserId is { } userId)
            claims.Add(new Claim(CitasClaimTypes.UsuarioId, userId.ToString()));
        if (!string.IsNullOrWhiteSpace(c.Nombre))
            claims.Add(new Claim(CitasClaimTypes.Nombre, c.Nombre));
        return claims;
    }
}
