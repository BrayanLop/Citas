using Citas.Application.Integracion;
using Citas.Application.Sincronizacion;

namespace Citas.Application.Acceso;

/// <summary>Alta y sincronización del personal de Natural como Usuarios de Citas.</summary>
public interface IPersonalAppService
{
    /// <summary>
    /// Devuelve el id del Usuario de Citas que representa a la persona de Natural en el
    /// tenant, creándolo (con rol Empleado) si aún no existe. Devuelve null si la empresa
    /// no tiene la agenda activada en Citas. Si el usuario existe pero está inactivo lanza
    /// <see cref="Citas.Domain.Common.ForbiddenAppException"/> (403).
    /// </summary>
    Task<int?> AsegurarUsuarioAsync(
        string tenant,
        int personaNaturalId,
        string? nombre,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upsert del personal del tenant a partir de las personas de Natural: crea (rol
    /// Empleado) o actualiza por PersonaNaturalId y deja inactivos a los que ya no están
    /// (salvo <paramref name="usuarioActualId"/>). Se ejecuta dentro de la transacción del llamador.
    /// </summary>
    Task<ResumenCambiosDto> SincronizarAsync(
        string tenant,
        IReadOnlyList<PersonaNatural> personas,
        int? usuarioActualId,
        CancellationToken cancellationToken = default);
}
