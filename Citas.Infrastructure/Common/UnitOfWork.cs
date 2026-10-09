using Citas.Application.Common;
using Citas.Data;
using Citas.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Citas.Infrastructure.Common;

/// <summary>Implementa <see cref="IUnitOfWork"/> sobre el <see cref="CitasContext"/>.</summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly CitasContext _db;

    public UnitOfWork(CitasContext db) => _db = db;

    public async Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
        {
            // La restricción de exclusión de Citas (EX_Citas_EmpleadoSinCruces) frenó un cruce
            // que se coló entre la validación y el guardado: es un conflicto, no un 500.
            _db.ChangeTracker.Clear();
            throw new ConflictException("El empleado ya tiene otra cita en ese horario. Elige otra hora.");
        }
    }

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
