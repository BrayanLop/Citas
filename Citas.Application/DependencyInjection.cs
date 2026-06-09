using Citas.Application.Agendamiento;
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
        return services;
    }
}
