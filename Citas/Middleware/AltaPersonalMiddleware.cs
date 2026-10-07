using System.Security.Claims;
using Citas.Application.Acceso;
using Citas.Application.Acceso.Abstractions;
using Citas.Infrastructure.Acceso;

namespace Citas.Middleware;

/// <summary>
/// Tras autenticar, si la petición viene con token de Natural (modo personal), asegura
/// que exista el Usuario de Citas para (tenant, personaNaturalId) —lo crea la primera
/// vez— y agrega su id como claim 'userId', igual que lo trae el token de un cliente.
/// Si la empresa aún no activó la agenda no se crea nada y las políticas de personal
/// responderán 403 (salvo "activar agenda").
/// </summary>
public class AltaPersonalMiddleware
{
    private readonly RequestDelegate _next;

    public AltaPersonalMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IUsuarioActual actual, IPersonalAppService personal)
    {
        if (actual.EsPersonal &&
            actual.Tenant is { } tenant &&
            actual.PersonaNaturalId is { } personaNaturalId &&
            context.User.Identity is ClaimsIdentity identidad)
        {
            var usuarioId = await personal.AsegurarUsuarioAsync(
                tenant, personaNaturalId, actual.Nombre, context.RequestAborted);

            if (usuarioId is { } id)
                identidad.AddClaim(new Claim(CitasClaimTypes.UsuarioId, id.ToString()));
        }

        await _next(context);
    }
}
