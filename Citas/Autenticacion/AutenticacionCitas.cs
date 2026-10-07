using System.Security.Claims;
using System.Text;
using Citas.Configuracion;
using Citas.Infrastructure.Acceso;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Citas.Autenticacion;

/// <summary>
/// Autenticación dual:
///  - "Natural": el personal de la empresa entra con el MISMO JWT que le emite Natural.
///    Al validarse, sus claims se traducen a los de Citas (tenant = empresaId,
///    personaNaturalId = sub, rolNatural = rol, mode = personal).
///  - "Citas": los clientes externos entran con el JWT que emite Citas en login /
///    select-empresa (mode = cliente).
/// El esquema por defecto ("Bearer") es un policy scheme que mira el 'iss' del token
/// (sin validarlo) y lo reenvía al esquema que corresponde; la validación real (firma,
/// issuer, audience, expiración) la hace siempre el esquema elegido.
/// </summary>
public static class AutenticacionCitas
{
    public const string EsquemaPorDefecto = JwtBearerDefaults.AuthenticationScheme;
    public const string EsquemaNatural = "Natural";
    public const string EsquemaCitas = "Citas";

    public static IServiceCollection AddAutenticacionCitas(this IServiceCollection services, IConfiguration config)
    {
        var naturalIssuer = ConfiguracionRequerida.NaturalIssuer(config);
        var naturalAudience = ConfiguracionRequerida.NaturalAudience(config);
        var jwt = config.GetSection("Jwt");

        services.AddAuthentication(EsquemaPorDefecto)
            .AddPolicyScheme(EsquemaPorDefecto, "Natural o Citas según el emisor", options =>
            {
                options.ForwardDefaultSelector = context =>
                    LeerIssuerSinValidar(context) == naturalIssuer ? EsquemaNatural : EsquemaCitas;
            })
            .AddJwtBearer(EsquemaNatural, options =>
            {
                // Sin mapeo: trabajamos con los nombres de claim tal como vienen en el token.
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = naturalIssuer,
                    ValidAudience = naturalAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["NaturalJwt:Key"]!)),
                    ClockSkew = TimeSpan.FromMinutes(2),
                };
                options.Events = new JwtBearerEvents { OnTokenValidated = TraducirTokenNatural };
            })
            .AddJwtBearer(EsquemaCitas, options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
                    NameClaimType = CitasClaimTypes.Nombre,
                    ClockSkew = TimeSpan.FromMinutes(2),
                };
                options.Events = new JwtBearerEvents { OnTokenValidated = ValidarTokenCliente };
            });

        return services;
    }

    /// <summary>
    /// Sustituye la identidad del token de Natural por una con los claims de Citas.
    /// Si faltan empresaId o sub (o no son enteros), el token se rechaza.
    /// </summary>
    private static Task TraducirTokenNatural(TokenValidatedContext context)
    {
        var origen = context.Principal!;

        var empresaId = Primero(origen, NaturalClaimTypes.EmpresaId);
        var personaId = Primero(origen, NaturalClaimTypes.PersonaId);
        if (!int.TryParse(empresaId, out var empresa) || empresa <= 0 ||
            !int.TryParse(personaId, out var persona) || persona <= 0)
        {
            context.Fail("El token de Natural no trae empresaId y sub válidos.");
            return Task.CompletedTask;
        }

        var claims = new List<Claim>
        {
            new(CitasClaimTypes.Modo, CitasClaimTypes.ModoPersonal),
            new(CitasClaimTypes.Tenant, empresa.ToString()),
            new(CitasClaimTypes.PersonaNaturalId, persona.ToString()),
        };
        if (Primero(origen, NaturalClaimTypes.Rol) is { } rol)
            claims.Add(new Claim(CitasClaimTypes.RolNatural, rol));
        if (Primero(origen, NaturalClaimTypes.Nombre) is { } nombre)
            claims.Add(new Claim(CitasClaimTypes.Nombre, nombre));

        context.Principal = new ClaimsPrincipal(new ClaimsIdentity(
            claims, EsquemaNatural, CitasClaimTypes.Nombre, CitasClaimTypes.RolNatural));
        return Task.CompletedTask;
    }

    /// <summary>Citas solo emite tokens de cliente; cualquier otro modo se rechaza.</summary>
    private static Task ValidarTokenCliente(TokenValidatedContext context)
    {
        if (context.Principal?.FindFirst(CitasClaimTypes.Modo)?.Value != CitasClaimTypes.ModoCliente)
            context.Fail("Token de Citas sin modo cliente.");
        return Task.CompletedTask;
    }

    private static string? Primero(ClaimsPrincipal principal, params string[] tipos)
        => tipos.Select(t => principal.FindFirst(t)?.Value).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static string? LeerIssuerSinValidar(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.ToString();
        const string prefijo = "Bearer ";
        if (!header.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
            return null;

        try
        {
            return new JsonWebToken(header[prefijo.Length..].Trim()).Issuer;
        }
        catch (Exception)
        {
            // Token mal formado: lo rechazará el esquema por defecto al validarlo.
            return null;
        }
    }
}
