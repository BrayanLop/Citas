namespace Citas.Application.Acceso;

/// <summary>Alta automática del personal de Natural como Usuario de Citas.</summary>
public interface IPersonalAppService
{
    /// <summary>
    /// Devuelve el id del Usuario de Citas que representa a la persona de Natural en el
    /// tenant, creándolo (con rol Empleado) si aún no existe. Devuelve null si la empresa
    /// no tiene la agenda activada en Citas.
    /// </summary>
    Task<int?> AsegurarUsuarioAsync(
        string tenant,
        int personaNaturalId,
        string? nombre,
        CancellationToken cancellationToken = default);
}
