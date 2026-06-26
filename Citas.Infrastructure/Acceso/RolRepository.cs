using Citas.Data;
using Citas.Domain.Acceso;
using Citas.Infrastructure.Persistencia;
using Citas.Infrastructure.Persistencia.Mapeadores;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Acceso;

public class RolRepository : IRolRepository
{
    private readonly CitasContext _db;

    public RolRepository(CitasContext db) => _db = db;

    public async Task<IReadOnlyList<Rol>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var filas = await _db.Roles.AsNoTracking()
            .OrderBy(r => r.NombreRol)
            .ToListAsync(cancellationToken);
        return filas.Select(RolMapper.ToDomain).ToList();
    }

    public async Task<Rol?> BuscarEnTenantPorNombreAsync(string tenant, string nombreRol, CancellationToken cancellationToken = default)
    {
        var fila = await _db.Roles.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(r => r.TenantId == tenant && r.NombreRol == nombreRol, cancellationToken);
        return fila is null ? null : RolMapper.ToDomain(fila);
    }

    public async Task AgregarAsync(Rol rol, string? tenant, CancellationToken cancellationToken = default)
    {
        var fila = RolMapper.ToDbModel(rol, tenant);
        await _db.Roles.AddAsync(fila, cancellationToken);
        _db.SincronizarIdTrasGuardar(() => rol.EstablecerIdentidad(fila.Id));
    }

    public Task<bool> ExisteAsignacionAsync(int usuarioId, int rolId, CancellationToken cancellationToken = default)
        => _db.RolUsuarios.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(ru => ru.IdUsuario == usuarioId && ru.IdRol == rolId, cancellationToken);

    public async Task AgregarAsignacionAsync(RolUsuario asignacion, string? tenant, CancellationToken cancellationToken = default)
        => await _db.RolUsuarios.AddAsync(RolUsuarioMapper.ToDbModel(asignacion, tenant), cancellationToken);
}
