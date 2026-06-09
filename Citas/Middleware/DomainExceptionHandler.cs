using Citas.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Middleware;

/// <summary>
/// Traduce las <see cref="AppException"/> (reglas de negocio, conflictos, no
/// autorizado…) a respuestas HTTP con ProblemDetails según el código que declara
/// cada excepción, para que los controladores no envuelvan cada llamada en try/catch.
/// </summary>
public class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not AppException appException)
            return false;

        var problem = new ProblemDetails
        {
            Status = appException.StatusCode,
            Title = appException.Title,
            Detail = appException.Message,
        };

        httpContext.Response.StatusCode = appException.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
