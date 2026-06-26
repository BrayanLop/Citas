using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citas.Infrastructure.Persistencia.Modelos;

[Table("Roles")]
public class RolDbModel : ITenantDbModel
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string NombreRol { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    public ICollection<RolUsuarioDbModel> RolUsuarios { get; set; } = [];
}
