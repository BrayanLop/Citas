using Citas.Application.Common;
using Citas.Domain.Acceso;
using Citas.Models;

namespace Citas.Application.Acceso;

/// <summary>Asegura que exista un rol en el tenant y lo asigna a un usuario.</summary>
internal static class AsignadorRoles
{
    public const string Cliente = "Cliente";
    public const string Empleado = "Empleado";

    public static async Task AsignarAsync(
        IRolRepository roles,
        IUnitOfWork uow,
        int usuarioId,
        string tenant,
        string nombreRol,
        CancellationToken cancellationToken)
    {
        var rol = await roles.BuscarEnTenantPorNombreAsync(tenant, nombreRol, cancellationToken);
        if (rol is null)
        {
            rol = Rol.Crear(nombreRol);
            await roles.AgregarAsync(rol, tenant, cancellationToken);
            await uow.GuardarCambiosAsync(cancellationToken);
        }

        if (!await roles.ExisteAsignacionAsync(usuarioId, rol.Id, cancellationToken))
        {
            await roles.AgregarAsignacionAsync(RolUsuario.Crear(usuarioId, rol.Id), tenant, cancellationToken);
            await uow.GuardarCambiosAsync(cancellationToken);
        }
    }
}
