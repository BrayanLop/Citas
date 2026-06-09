namespace Citas.Application.Acceso.Abstractions;

/// <summary>Datos que se incrustan en el JWT emitido por Citas.</summary>
public record TokenClaims(
    string? Tenant = null,
    int? PersonaId = null,
    int? UserId = null,
    string? Nombre = null,
    string? Modo = null);

/// <summary>
/// Puerto para emitir y validar tokens. La implementación (firma JWT con la clave
/// configurada) vive en la infraestructura.
/// </summary>
public interface ITokenService
{
    (string Token, DateTime ExpiraUtc) Generar(TokenClaims claims, int horas = 8);

    /// <summary>
    /// Valida el token externo (Natural) según la política configurada: si no hay clave
    /// compartida configurada se considera válido; si la hay, el token debe ser correcto.
    /// </summary>
    bool EmpleadoExternoEsValido(string? naturalToken);
}
