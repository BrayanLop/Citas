using Citas.Domain.Common;

namespace Citas.Application.Integracion;

/// <summary>
/// Error al consultar la API de Natural, ya traducido al código HTTP con el que responde
/// Citas: 403 si Natural rechaza el token (401/403 allá) y 502 si Natural está caído, tarda
/// demasiado o responde algo que no se puede interpretar.
/// </summary>
public sealed class IntegracionNaturalException : AppException
{
    private IntegracionNaturalException(int statusCode, string title, string message) : base(message)
    {
        StatusCode = statusCode;
        Title = title;
    }

    public override int StatusCode { get; }
    public override string Title { get; }

    public static IntegracionNaturalException SinPermiso(string message)
        => new(403, "Natural rechazó la petición", message);

    public static IntegracionNaturalException NoDisponible(string message)
        => new(502, "Natural no disponible", message);
}
