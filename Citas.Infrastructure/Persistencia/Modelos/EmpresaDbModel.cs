using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citas.Infrastructure.Persistencia.Modelos;

// Catálogo de empresas/tenants. No lleva TenantId: es la tabla raíz de los tenants.
[Table("Empresas")]
public class EmpresaDbModel
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    public bool Activo { get; set; }
}
