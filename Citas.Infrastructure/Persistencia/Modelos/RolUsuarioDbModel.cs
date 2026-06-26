using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citas.Infrastructure.Persistencia.Modelos;

[Table("RolUsuarios")]
public class RolUsuarioDbModel : ITenantDbModel
{
    [Key]
    public int IdRolUsuario { get; set; }

    public int IdUsuario { get; set; }
    public int IdRol { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    public UsuarioDbModel? Usuario { get; set; }
    public RolDbModel? Rol { get; set; }
}
