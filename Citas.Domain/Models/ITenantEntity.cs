namespace Citas.Models;

/// <summary>
/// Marca una entidad como perteneciente a un tenant (empresa). El valor de
/// <see cref="TenantId"/> aisla los registros de cada empresa dentro de tablas compartidas.
/// </summary>
public interface ITenantEntity
{
    /// <summary>Codigo de la empresa/tenant dueña del registro.</summary>
    string TenantId { get; set; }
}
