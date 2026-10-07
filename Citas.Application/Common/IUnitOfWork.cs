namespace Citas.Application.Common;

/// <summary>
/// Confirma en una sola transacción los cambios acumulados por los repositorios.
/// Desacopla los servicios de aplicación del DbContext de EF Core.
/// </summary>
public interface IUnitOfWork
{
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Descarta los cambios pendientes (p. ej. tras un fallo al guardar por una carrera
    /// con otra petición), para que el siguiente guardado no los reintente.
    /// </summary>
    void DescartarCambios();
}
