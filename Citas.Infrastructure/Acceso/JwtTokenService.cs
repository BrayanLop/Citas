using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Citas.Application.Acceso.Abstractions;
using Citas.Multitenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Citas.Infrastructure.Acceso;

/// <summary>Adaptador del puerto <see cref="ITokenService"/> con JWT firmado (HMAC-SHA256).</summary>
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

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: ConstruirClaims(claims),
            expires: expira,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }

    public bool EmpleadoExternoEsValido(string? naturalToken)
    {
        var clave = _config["NaturalJwt:Key"];
        // Sin clave compartida configurada, la validación está deshabilitada.
        if (string.IsNullOrWhiteSpace(clave)) return true;
        if (string.IsNullOrWhiteSpace(naturalToken)) return false;

        try
        {
            new JwtSecurityTokenHandler().ValidateToken(naturalToken, new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(clave)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2),
            }, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<Claim> ConstruirClaims(TokenClaims c)
    {
        var claims = new List<Claim>();
        if (!string.IsNullOrWhiteSpace(c.Tenant))
            claims.Add(new Claim(TenantClaimTypes.Tenant, c.Tenant));
        if (c.PersonaId is { } personaId)
            claims.Add(new Claim("personaId", personaId.ToString()));
        if (c.UserId is { } userId)
            claims.Add(new Claim("userId", userId.ToString()));
        if (!string.IsNullOrWhiteSpace(c.Nombre))
            claims.Add(new Claim(ClaimTypes.Name, c.Nombre));
        if (!string.IsNullOrWhiteSpace(c.Modo))
            claims.Add(new Claim("mode", c.Modo));
        return claims;
    }
}
