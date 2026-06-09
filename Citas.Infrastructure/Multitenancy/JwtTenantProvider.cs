using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Citas.Multitenancy;

/// <summary>
/// Obtiene el tenant a partir del claim 'tenant' del usuario autenticado (JWT).
/// </summary>
public class JwtTenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtTenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetTenantId()
        => _httpContextAccessor.HttpContext?.User.FindFirstValue(TenantClaimTypes.Tenant);
}
