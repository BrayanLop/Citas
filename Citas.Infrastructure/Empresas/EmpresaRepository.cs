using Citas.Data;
using Citas.Domain.Empresas;
using Citas.Infrastructure.Persistencia.Mapeadores;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Empresas;

public class EmpresaRepository : IEmpresaRepository
{
    private readonly CitasContext _db;

    public EmpresaRepository(CitasContext db) => _db = db;

    public async Task<IReadOnlyList<Empresa>> ListarActivasAsync(CancellationToken cancellationToken = default)
    {
        var filas = await _db.Empresas.AsNoTracking()
            .Where(e => e.Activo)
            .OrderBy(e => e.Nombre)
            .ToListAsync(cancellationToken);
        return filas.Select(EmpresaMapper.ToDomain).ToList();
    }

    public async Task<Empresa?> ObtenerPorIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var fila = await _db.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        return fila is null ? null : EmpresaMapper.ToDomain(fila);
    }

    public async Task AgregarAsync(Empresa empresa, CancellationToken cancellationToken = default)
        => await _db.Empresas.AddAsync(EmpresaMapper.ToDbModel(empresa), cancellationToken);

    public void Actualizar(Empresa empresa) => _db.Empresas.Update(EmpresaMapper.ToDbModel(empresa));
}
