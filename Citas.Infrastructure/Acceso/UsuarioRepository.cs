using Citas.Data;
using Citas.Domain.Acceso;
using Citas.Infrastructure.Persistencia;
using Citas.Infrastructure.Persistencia.Mapeadores;
using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Acceso;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly CitasContext _db;

    public UsuarioRepository(CitasContext db) => _db = db;

    public async Task<IReadOnlyList<Usuario>> ListarConRolesAsync(CancellationToken cancellationToken = default)
    {
        var filas = await _db.Usuarios
            .AsNoTracking()
            .Include(u => u.RolUsuarios)
                .ThenInclude(ru => ru.Rol)
            .ToListAsync(cancellationToken);

        return filas.Select(UsuarioMapper.ToDomain).ToList();
    }

    public async Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var fila = await _db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return fila is null ? null : UsuarioMapper.ToDomain(fila);
    }

    public async Task AgregarAsync(Usuario usuario, string? tenant, CancellationToken cancellationToken = default)
    {
        var fila = UsuarioMapper.ToDbModel(usuario, tenant);
        await _db.Usuarios.AddAsync(fila, cancellationToken);
        _db.SincronizarIdTrasGuardar(() => usuario.EstablecerIdentidad(fila.Id));
    }

    public void Eliminar(Usuario usuario)
        => _db.Usuarios.Remove(new UsuarioDbModel { Id = usuario.Id });

    public async Task<Usuario?> BuscarEnTenantPorPersonaAsync(int personaId, string tenant, CancellationToken cancellationToken = default)
    {
        var fila = await _db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(u => u.PersonaId == personaId && u.TenantId == tenant, cancellationToken);
        return fila is null ? null : UsuarioMapper.ToDomain(fila);
    }

    public async Task<Usuario?> BuscarEnTenantPorPersonaNaturalAsync(string tenant, int personaNaturalId, CancellationToken cancellationToken = default)
    {
        var fila = await _db.Usuarios.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(u => u.TenantId == tenant && u.PersonaNaturalId == personaNaturalId, cancellationToken);
        return fila is null ? null : UsuarioMapper.ToDomain(fila);
    }
}
