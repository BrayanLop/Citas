using Citas.Application.Calendario;
using Citas.Multitenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Citas.Infrastructure.Calendario;

/// <summary>
/// Cuando no hay petición con token (proceso en segundo plano, callback de Google, webhook), el
/// tenant se fija aquí. <see cref="JwtTenantProvider"/> lo consulta antes que los claims.
/// Es scoped: vale solo dentro del alcance que lo creó.
/// </summary>
public sealed class TenantEnEjecucion
{
    public string? Tenant { get; set; }
}

/// <summary>Crea un alcance propio con el tenant ya fijado y resuelve el servicio dentro de él.</summary>
public sealed class EjecutorPorTenant : IEjecutorPorTenant
{
    private readonly IServiceScopeFactory _fabrica;

    public EjecutorPorTenant(IServiceScopeFactory fabrica) => _fabrica = fabrica;

    public async Task EjecutarAsync<TServicio>(string tenant, Func<TServicio, CancellationToken, Task> accion, CancellationToken ct = default)
        where TServicio : notnull
    {
        using var alcance = _fabrica.CreateScope();
        // Primero el tenant: el DbContext lo lee al construirse, o sea al resolver el servicio.
        alcance.ServiceProvider.GetRequiredService<TenantEnEjecucion>().Tenant = tenant;
        var servicio = alcance.ServiceProvider.GetRequiredService<TServicio>();
        await accion(servicio, ct);
    }
}
