using Citas.Data;
using Citas.Domain.Acceso;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Acceso;

public class RolRepository : IRolRepository
{
    private readonly CitasContext _db;

    public RolRepository(CitasContext db) => _db = db;

    public async Task<IReadOnlyList<Rol>> ListarAsync(CancellationToken cancellationToken = default)
        => await _db.Roles.OrderBy(r => r.NombreRol).ToListAsync(cancellationToken);

    public async Task<Rol?> BuscarEnTenantPorNombreAsync(string tenant, string nombreRol, CancellationToken cancellationToken = default)
        => await _db.Roles.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.TenantId == tenant && r.NombreRol == nombreRol, cancellationToken);

    public async Task AgregarAsync(Rol rol, CancellationToken cancellationToken = default)
        => await _db.Roles.AddAsync(rol, cancellationToken);

    public async Task<bool> ExisteAsignacionAsync(int usuarioId, int rolId, CancellationToken cancellationToken = default)
        => await _db.RolUsuarios.IgnoreQueryFilters()
            .AnyAsync(ru => ru.IdUsuario == usuarioId && ru.IdRol == rolId, cancellationToken);

    public async Task AgregarAsignacionAsync(RolUsuario asignacion, CancellationToken cancellationToken = default)
        => await _db.RolUsuarios.AddAsync(asignacion, cancellationToken);
}
