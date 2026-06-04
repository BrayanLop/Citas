namespace Citas.Models;

/// <summary>
/// Catalogo de empresas/tenants. El <see cref="Id"/> es el codigo que se usa
/// como TenantId en el resto de las tablas y que viaja en el claim 'tenant' del JWT.
/// </summary>
public class Empresa
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
}
