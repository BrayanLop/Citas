using Citas.Data;
using Citas.Domain.Acceso;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Acceso;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly CitasContext _db;

    public UsuarioRepository(CitasContext db) => _db = db;

    public async Task<IReadOnlyList<Usuario>> ListarConRolesAsync(CancellationToken cancellationToken = default)
        => await _db.Usuarios
            .Include(u => u.RolUsuarios)
                .ThenInclude(ru => ru.Rol)
            .ToListAsync(cancellationToken);

    public async Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        => await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        => await _db.Usuarios.AddAsync(usuario, cancellationToken);

    public void Eliminar(Usuario usuario) => _db.Usuarios.Remove(usuario);

    public async Task<Usuario?> BuscarEnTenantPorPersonaAsync(int personaId, string tenant, CancellationToken cancellationToken = default)
        => await _db.Usuarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PersonaId == personaId && u.TenantId == tenant, cancellationToken);

    public async Task<Usuario?> BuscarEnTenantPorNombreAsync(string tenant, string nombre, CancellationToken cancellationToken = default)
    {
        var nombreLower = nombre.ToLower();
        return await _db.Usuarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenant && u.NombreUsuario.ToLower() == nombreLower, cancellationToken);
    }
}
