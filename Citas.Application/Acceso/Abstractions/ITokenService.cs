namespace Citas.Application.Acceso.Abstractions;

/// <summary>Datos que se incrustan en el JWT de cliente emitido por Citas.</summary>
public record TokenClaims(
    string? Tenant = null,
    int? PersonaId = null,
    int? UserId = null,
    string? Nombre = null);

/// <summary>
/// Puerto para emitir los tokens de los clientes externos. La implementación (firma JWT
/// con la clave configurada) vive en la infraestructura. El personal de la empresa no
/// recibe tokens de Citas: usa directamente el token de Natural.
/// </summary>
public interface ITokenService
{
    (string Token, DateTime ExpiraUtc) Generar(TokenClaims claims, int horas = 8);
}
