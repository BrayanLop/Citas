using Citas.Application.Acceso;
using Citas.Application.Agendamiento;
using Citas.Application.Empresas;
using Citas.Application.Servicios;
using Microsoft.Extensions.DependencyInjection;

namespace Citas.Application;

/// <summary>
/// Registro de la capa de aplicación: servicios de casos de uso.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICitaAppService, CitaAppService>();
        services.AddScoped<IAgendaEmpleadoAppService, AgendaEmpleadoAppService>();
        services.AddScoped<IEmpresaAppService, EmpresaAppService>();
        services.AddScoped<IServicioAppService, ServicioAppService>();
        services.AddScoped<IUsuarioAppService, UsuarioAppService>();
        services.AddScoped<IRolAppService, RolAppService>();
        services.AddScoped<IAuthAppService, AuthAppService>();
        services.AddScoped<IPersonalAppService, PersonalAppService>();
        return services;
    }
}
