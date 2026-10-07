namespace Citas.Domain.Common;

/// <summary>
/// Base de las excepciones de negocio/aplicación. Cada subtipo declara el código
/// HTTP con el que la capa de presentación debe responder, de modo que los
/// controladores no necesiten traducir errores manualmente.
/// </summary>
public abstract class AppException : Exception
{
    public abstract int StatusCode { get; }
    public abstract string Title { get; }

    protected AppException(string message) : base(message) { }
}

/// <summary>Recurso ya existente o estado en conflicto (HTTP 409).</summary>
public sealed class ConflictException : AppException
{
    public override int StatusCode => 409;
    public override string Title => "Conflicto";
    public ConflictException(string message) : base(message) { }
}

/// <summary>Credenciales inválidas o ausentes (HTTP 401).</summary>
public sealed class UnauthorizedAppException : AppException
{
    public override int StatusCode => 401;
    public override string Title => "No autorizado";
    public UnauthorizedAppException(string message) : base(message) { }
}

/// <summary>Autenticado pero sin permiso para la operación (HTTP 403).</summary>
public sealed class ForbiddenAppException : AppException
{
    public override int StatusCode => 403;
    public override string Title => "Prohibido";
    public ForbiddenAppException(string message) : base(message) { }
}
