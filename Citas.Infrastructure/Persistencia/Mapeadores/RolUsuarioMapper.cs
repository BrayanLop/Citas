using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Models;

namespace Citas.Infrastructure.Persistencia.Mapeadores;

internal static class RolUsuarioMapper
{
    public static RolUsuario ToDomain(RolUsuarioDbModel db)
        => RolUsuario.Rehidratar(
            db.IdRolUsuario, db.IdUsuario, db.IdRol,
            db.Rol is null ? null : RolMapper.ToDomain(db.Rol));

    public static RolUsuarioDbModel ToDbModel(RolUsuario ru, string? tenant = null)
        => new()
        {
            IdRolUsuario = ru.IdRolUsuario,
            IdUsuario = ru.IdUsuario,
            IdRol = ru.IdRol,
            TenantId = tenant ?? string.Empty,
        };
}
