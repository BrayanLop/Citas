using Citas.Application.Common;
using Citas.Data;
using Citas.Domain.Agendamiento;
using Citas.Infrastructure.Agendamiento;
using Citas.Infrastructure.Common;
using Citas.Multitenancy;
using Citas.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Citas.Infrastructure;

/// <summary>
/// Registro de la capa de infraestructura: acceso a datos (EF Core + PostgreSQL),
/// repositorios, multitenancy y servicios de persistencia.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CitasContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Repositorio genérico (estilo previo a DDD) para los controladores aún no migrados.
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Agregado Cita (DDD): repositorio dedicado + unidad de trabajo.
        services.AddScoped<ICitaRepository, CitaRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Multitenancy: el tenant se resuelve desde el claim 'tenant' del JWT.
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, JwtTenantProvider>();

        return services;
    }
}
