using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Models;

namespace Citas.Infrastructure.Persistencia.Mapeadores;

internal static class UsuarioMapper
{
    public static Usuario ToDomain(UsuarioDbModel db)
        => Usuario.Rehidratar(
            db.Id,
            db.NombreUsuario,
            db.ClaveHash,
            db.Estado,
            db.PersonaId,
            db.PersonaNaturalId,
            // Los roles solo vienen cuando el repositorio hizo el Include correspondiente.
            db.RolUsuarios.Count == 0 ? null : db.RolUsuarios.Select(RolUsuarioMapper.ToDomain));

    public static UsuarioDbModel ToDbModel(Usuario u, string? tenant = null)
        => new()
        {
            Id = u.Id,
            NombreUsuario = u.NombreUsuario,
            ClaveHash = u.ClaveHash,
            Estado = u.Estado,
            PersonaId = u.PersonaId,
            PersonaNaturalId = u.PersonaNaturalId,
            TenantId = tenant ?? string.Empty,
        };
}
