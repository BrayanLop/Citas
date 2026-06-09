namespace Citas.Application.Common;

/// <summary>
/// Confirma en una sola transacción los cambios acumulados por los repositorios.
/// Desacopla los servicios de aplicación del DbContext de EF Core.
/// </summary>
public interface IUnitOfWork
{
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
