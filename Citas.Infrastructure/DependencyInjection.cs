using Citas.Application.Acceso.Abstractions;
using Citas.Application.Agendamiento;
using Citas.Application.Common;
using Citas.Application.Integracion;
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
using Citas.Infrastructure.Integracion;
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

        // Multitenancy: el tenant se resuelve desde el claim 'tenant' del JWT (el de
        // Natural se normaliza a ese claim al validarse; ver Program.cs).
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, JwtTenantProvider>();

        // Usuario de la petición (modo personal/cliente, rol de Natural, usuario de Citas).
        services.AddScoped<IUsuarioActual, UsuarioActual>();

        // API de Natural (catálogo de servicios y personal) para la sincronización.
        services.AddNaturalApi(configuration);

        // Zona horaria, intervalo y anticipación de la agenda.
        services.AddAgenda(configuration);

        return services;
    }

    // Agenda:ZonaHoraria (por defecto America/Bogota; si el sistema no la tiene, UTC-5 fijo),
    // Agenda:IntervaloMinutos (por defecto 15) y Agenda:AnticipacionMinimaMinutos (por defecto 0).
    // La zona se resuelve aquí para que un id inválido haga fallar el arranque.
    private static IServiceCollection AddAgenda(this IServiceCollection services, IConfiguration configuration)
    {
        var seccion = configuration.GetSection("Agenda");
        services.AddSingleton(new OpcionesAgenda
        {
            ZonaHoraria = OpcionesAgenda.ResolverZona(seccion["ZonaHoraria"]),
            IntervaloMinutos = seccion.GetValue<int?>("IntervaloMinutos") is > 0 and var intervalo
                ? intervalo
                : OpcionesAgenda.IntervaloMinutosPorDefecto,
            AnticipacionMinimaMinutos = seccion.GetValue<int?>("AnticipacionMinimaMinutos") is >= 0 and var anticipacion
                ? anticipacion
                : 0,
        });
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<RelojAgenda>();
        return services;
    }

    // NaturalApi:BaseUrl (obligatoria, la valida el arranque), NaturalApi:TimeoutSegundos
    // (por defecto 15) y NaturalApi:TiempoEstimadoPorDefecto (minutos, por defecto 30).
    private static IServiceCollection AddNaturalApi(this IServiceCollection services, IConfiguration configuration)
    {
        var seccion = configuration.GetSection("NaturalApi");
        var timeout = seccion.GetValue<int?>("TimeoutSegundos") is > 0 and var t ? t : 15;

        services.AddHttpClient<INaturalCatalogo, NaturalCatalogoHttp>(client =>
        {
            // Con barra final para que "Servicio/Obtener" se resuelva debajo de la base (.../v2/api/).
            var baseUrl = seccion["BaseUrl"]
                ?? throw new InvalidOperationException("Falta NaturalApi__BaseUrl.");
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(timeout);
        });

        services.AddSingleton(new OpcionesSincronizacion
        {
            TiempoEstimadoPorDefecto = seccion.GetValue<int?>("TiempoEstimadoPorDefecto") is > 0 and var minutos
                ? minutos
                : OpcionesSincronizacion.TiempoEstimadoPorDefectoInicial,
        });

        return services;
    }
}
