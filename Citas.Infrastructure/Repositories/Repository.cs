using System.Linq.Expressions;
using Citas.Data;
using Microsoft.EntityFrameworkCore;

namespace Citas.Repositories;

/// <summary>
/// Implementación genérica de <see cref="IRepository{T}"/> sobre Entity Framework Core.
/// Encapsula el <see cref="CitasContext"/> para que las capas superiores no lo conozcan.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly CitasContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(CitasContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> query = _dbSet;
        foreach (var include in includes)
            query = query.Include(include);

        return await query.ToListAsync();
    }

    public async Task<T?> GetByIdAsync(params object[] keyValues)
        => await _dbSet.FindAsync(keyValues);

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.AnyAsync(predicate);

    public async Task AddAsync(T entity)
        => await _dbSet.AddAsync(entity);

    public void Update(T entity)
        => _dbSet.Update(entity);

    public void Remove(T entity)
        => _dbSet.Remove(entity);

    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
