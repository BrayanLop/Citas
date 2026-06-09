namespace Citas.Domain.Common;

/// <summary>
/// Error de regla de negocio (invariante de dominio violada). La capa de
/// presentación lo traduce a una respuesta HTTP 400.
/// </summary>
public class DomainException : AppException
{
    public override int StatusCode => 400;
    public override string Title => "Regla de negocio no cumplida";
    public DomainException(string message) : base(message) { }
}
