using Citas.Data;
using Citas.Domain.Empresas;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Empresas;

public class EmpresaRepository : IEmpresaRepository
{
    private readonly CitasContext _db;

    public EmpresaRepository(CitasContext db) => _db = db;

    public async Task<IReadOnlyList<Empresa>> ListarAsync(CancellationToken cancellationToken = default)
        => await _db.Empresas.OrderBy(e => e.Nombre).ToListAsync(cancellationToken);

    public async Task<Empresa?> ObtenerPorIdAsync(string id, CancellationToken cancellationToken = default)
        => await _db.Empresas.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<bool> ExisteAsync(string id, CancellationToken cancellationToken = default)
        => await _db.Empresas.AnyAsync(e => e.Id == id, cancellationToken);

    public async Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default)
        => await _db.Empresas.AddAsync(empresa, cancellationToken);
}
