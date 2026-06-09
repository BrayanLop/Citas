namespace Citas.Multitenancy;

/// <summary>
/// Resuelve el tenant (empresa) de la peticion en curso. La implementacion
/// determina de donde se obtiene (claim del JWT, header, subdominio, etc.).
/// </summary>
public interface ITenantProvider
{
    /// <summary>Codigo del tenant actual, o null si no se pudo determinar.</summary>
    string? GetTenantId();
}
