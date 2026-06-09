using Citas.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Middleware;

/// <summary>
/// Traduce las <see cref="DomainException"/> (invariantes de negocio violadas) a
/// respuestas HTTP 400 con ProblemDetails, para que los controladores no tengan
/// que envolver cada llamada en try/catch.
/// </summary>
public class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
            return false;

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Regla de negocio no cumplida",
            Detail = domainException.Message,
        };

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
