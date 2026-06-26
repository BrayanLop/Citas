namespace Citas.Infrastructure.Persistencia;

// Marca un modelo de persistencia como "vive dentro de un tenant".
// El TenantId lo sella el DbContext en SaveChanges y lo usan los filtros globales.
// Es una preocupación de base de datos, así que vive en Infrastructure (no en el dominio).
public interface ITenantDbModel
{
    string TenantId { get; set; }
}
