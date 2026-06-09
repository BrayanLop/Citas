namespace Citas.Domain.Common;

/// <summary>
/// Error de regla de negocio (invariante de dominio violada). La capa de
/// presentación lo traduce a una respuesta HTTP 400.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
