using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Models;

namespace Citas.Infrastructure.Persistencia.Mapeadores;

internal static class RolMapper
{
    public static Rol ToDomain(RolDbModel db)
        => Rol.Rehidratar(db.Id, db.NombreRol);

    // El tenant no está en el dominio: lo pasa la capa de aplicación cuando hace falta.
    public static RolDbModel ToDbModel(Rol r, string? tenant = null)
        => new()
        {
            Id = r.Id,
            NombreRol = r.NombreRol,
            TenantId = tenant ?? string.Empty,
        };
}
