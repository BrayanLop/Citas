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

    public void DescartarCambios() => _db.ChangeTracker.Clear();
}
