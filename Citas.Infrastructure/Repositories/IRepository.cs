using System.Linq.Expressions;

namespace Citas.Repositories;

/// <summary>
/// Abstracción genérica para el acceso a datos. Permite que los controladores
/// (u otras capas) trabajen contra esta interfaz en lugar de depender
/// directamente del DbContext de Entity Framework Core.
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>Obtiene todas las entidades, opcionalmente cargando relaciones.</summary>
    Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);

    /// <summary>Busca una entidad por su(s) llave(s) primaria(s).</summary>
    Task<T?> GetByIdAsync(params object[] keyValues);

    /// <summary>Indica si existe al menos una entidad que cumpla el predicado.</summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    /// <summary>Agrega una nueva entidad al contexto (no persiste hasta SaveChangesAsync).</summary>
    Task AddAsync(T entity);

    /// <summary>Marca una entidad como modificada.</summary>
    void Update(T entity);

    /// <summary>Marca una entidad para eliminación.</summary>
    void Remove(T entity);

    /// <summary>Persiste todos los cambios pendientes en la base de datos.</summary>
    Task<int> SaveChangesAsync();
}
