using System.Security.Claims;
using Citas.Application.Acceso.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Citas.Infrastructure.Acceso;

/// <summary>
/// Adaptador de <see cref="IUsuarioActual"/> sobre los claims normalizados de la
/// petición (<see cref="CitasClaimTypes"/>). Lee el principal en cada acceso, así ve el
/// userId que el middleware de alta del personal agrega tras autenticar.
/// </summary>
public class UsuarioActual : IUsuarioActual
{
    private readonly Func<ClaimsPrincipal?> _principal;

    public UsuarioActual(IHttpContextAccessor httpContextAccessor)
        : this(() => httpContextAccessor.HttpContext?.User) { }

    private UsuarioActual(Func<ClaimsPrincipal?> principal) => _principal = principal;

    /// <summary>Vista de un principal concreto (la usan las políticas de autorización).</summary>
    public static IUsuarioActual Desde(ClaimsPrincipal? principal) => new UsuarioActual(() => principal);

    private ClaimsPrincipal? User => _principal();

    public ModoAcceso Modo
    {
        get
        {
            if (User?.Identity?.IsAuthenticated != true)
                return ModoAcceso.Anonimo;
            return Valor(CitasClaimTypes.Modo) switch
            {
                CitasClaimTypes.ModoPersonal => ModoAcceso.Personal,
                CitasClaimTypes.ModoCliente => ModoAcceso.Cliente,
                _ => ModoAcceso.Anonimo,
            };
        }
    }

    public string? Tenant => Valor(CitasClaimTypes.Tenant);
    public int? UsuarioId => Entero(CitasClaimTypes.UsuarioId);
    public int? PersonaId => Entero(CitasClaimTypes.PersonaId);
    public int? PersonaNaturalId => Entero(CitasClaimTypes.PersonaNaturalId);
    public string? RolNatural => Valor(CitasClaimTypes.RolNatural);
    public string? Nombre => Valor(CitasClaimTypes.Nombre);

    private string? Valor(string tipo)
    {
        var valor = User?.FindFirst(tipo)?.Value;
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }

    private int? Entero(string tipo) => int.TryParse(Valor(tipo), out var n) ? n : null;
}
