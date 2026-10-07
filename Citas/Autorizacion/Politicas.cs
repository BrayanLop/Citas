using Citas.Application.Acceso.Abstractions;
using Citas.Infrastructure.Acceso;

namespace Citas.Autorizacion;

/// <summary>
/// Políticas de autorización por modo de acceso, en un solo sitio. Los controladores solo
/// eligen la política; las reglas por registro (p. ej. "solo sus citas") viven en los
/// casos de uso, que consultan <see cref="IUsuarioActual"/>.
///
/// Resumen:
///  - Personal: token de Natural con la agenda de su empresa activada (ya tiene Usuario de Citas).
///  - Administrador: Personal con rol de Natural '01' o '03'.
///  - ActivarAgenda: token de Natural con rol '01'/'03' (la empresa puede no existir aún).
///  - ClienteOAdministrador: cliente en un tenant o personal administrativo.
///  - MiembroTenant: cualquier personal o cliente dentro de un tenant.
///  - ClienteGlobal: cliente con login (aún sin empresa elegida).
/// Si no se cumple, la respuesta es 403 (o 401 sin token).
/// </summary>
public static class Politicas
{
    public const string Personal = nameof(Personal);
    public const string Administrador = nameof(Administrador);
    public const string ActivarAgenda = nameof(ActivarAgenda);
    public const string ClienteOAdministrador = nameof(ClienteOAdministrador);
    public const string MiembroTenant = nameof(MiembroTenant);
    public const string ClienteGlobal = nameof(ClienteGlobal);

    public static IServiceCollection AddPoliticasCitas(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Personal, p => p.RequireAssertion(c => EsPersonalEnTenant(Usuario(c))))
            .AddPolicy(Administrador, p => p.RequireAssertion(c => EsAdministradorEnTenant(Usuario(c))))
            .AddPolicy(ActivarAgenda, p => p.RequireAssertion(c =>
            {
                var u = Usuario(c);
                return u.EsAdministrador && u.Tenant is not null;
            }))
            .AddPolicy(ClienteOAdministrador, p => p.RequireAssertion(c =>
            {
                var u = Usuario(c);
                return EsClienteEnTenant(u) || EsAdministradorEnTenant(u);
            }))
            .AddPolicy(MiembroTenant, p => p.RequireAssertion(c =>
            {
                var u = Usuario(c);
                return EsClienteEnTenant(u) || EsPersonalEnTenant(u);
            }))
            .AddPolicy(ClienteGlobal, p => p.RequireAssertion(c =>
            {
                var u = Usuario(c);
                return u.EsCliente && u.PersonaId is not null;
            }));

        return services;
    }

    private static IUsuarioActual Usuario(Microsoft.AspNetCore.Authorization.AuthorizationHandlerContext c)
        => UsuarioActual.Desde(c.User);

    // El personal solo opera cuando ya tiene Usuario en el tenant (empresa activada).
    private static bool EsPersonalEnTenant(IUsuarioActual u)
        => u.EsPersonal && u.Tenant is not null && u.UsuarioId is not null;

    private static bool EsAdministradorEnTenant(IUsuarioActual u)
        => EsPersonalEnTenant(u) && u.EsAdministrador;

    private static bool EsClienteEnTenant(IUsuarioActual u)
        => u.EsCliente && u.Tenant is not null && u.UsuarioId is not null;
}
