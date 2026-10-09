using Citas.Application.Common;
using Citas.Data;

namespace Citas.Infrastructure.Common;

/// <summary>Implementa <see cref="IUnitOfWork"/> sobre el <see cref="CitasContext"/>.</summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly CitasContext _db;

    public UnitOfWork(CitasContext db) => _db = db;

    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);

    public async Task<T> EjecutarEnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancellationToken = default)
    {
        await using var transaccion = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var resultado = await operacion(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
            return resultado;
        }
        catch
        {
            // El rollback lo hace la transacción al liberarse; además se limpia el tracker
            // para que nada de lo que falló quede pendiente en esta petición.
            _db.ChangeTracker.Clear();
            throw;
        }
    }

    public void DescartarCambios() => _db.ChangeTracker.Clear();
}
