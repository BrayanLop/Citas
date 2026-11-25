using System.Security.Claims;
using Citas.Infrastructure.Calendario;
using Microsoft.AspNetCore.Http;

namespace Citas.Multitenancy;

/// <summary>
/// Obtiene el tenant a partir del claim 'tenant' del usuario autenticado (JWT). Fuera de una
/// petición con token (proceso en segundo plano, webhook) lo fija <see cref="TenantEnEjecucion"/>.
/// </summary>
public class JwtTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly TenantEnEjecucion _enEjecucion;

    public JwtTenantProvider(IHttpContextAccessor httpContextAccessor, TenantEnEjecucion enEjecucion)
    {
        _httpContextAccessor = httpContextAccessor;
        _enEjecucion = enEjecucion;
    }

    public string? GetTenantId()
        => _enEjecucion.Tenant ?? _httpContextAccessor.HttpContext?.User.FindFirstValue(TenantClaimTypes.Tenant);
}
