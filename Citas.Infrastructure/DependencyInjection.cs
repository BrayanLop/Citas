using Citas.Application.Acceso.Abstractions;
using Citas.Application.Common;
using Citas.Application.Diagnostico;
using Citas.Data;
using Citas.Domain.Acceso;
using Citas.Domain.Agendamiento;
using Citas.Domain.Empresas;
using Citas.Domain.Servicios;
using Citas.Infrastructure.Acceso;
using Citas.Infrastructure.Agendamiento;
using Citas.Infrastructure.Common;
using Citas.Infrastructure.Diagnostico;
using Citas.Infrastructure.Empresas;
using Citas.Infrastructure.Servicios;
using Citas.Multitenancy;
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

        // Repositorios por agregado (DDD) + unidad de trabajo.
        services.AddScoped<ICitaRepository, CitaRepository>();
        services.AddScoped<IAgendaEmpleadoRepository, AgendaEmpleadoRepository>();
        services.AddScoped<IEmpresaRepository, EmpresaRepository>();
        services.AddScoped<IServicioRepository, ServicioRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRolRepository, RolRepository>();
        services.AddScoped<IPersonaRepository, PersonaRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Servicios de identidad (puertos de Acceso).
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();

        // Diagnóstico (health check).
        services.AddScoped<IHealthAppService, HealthAppService>();

        // Multitenancy: el tenant se resuelve desde el claim 'tenant' del JWT.
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, JwtTenantProvider>();

        return services;
    }
}
