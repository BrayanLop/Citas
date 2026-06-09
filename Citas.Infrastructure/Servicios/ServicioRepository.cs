using Citas.Data;
using Citas.Domain.Servicios;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Servicios;

public class ServicioRepository : IServicioRepository
{
    private readonly CitasContext _db;

    public ServicioRepository(CitasContext db) => _db = db;

    public async Task<IReadOnlyList<Servicio>> ListarAsync(CancellationToken cancellationToken = default)
        => await _db.Servicios.OrderBy(s => s.NombreServicio).ToListAsync(cancellationToken);

    public async Task<Servicio?> ObtenerPorIdAsync(int idServicio, CancellationToken cancellationToken = default)
        => await _db.Servicios.FirstOrDefaultAsync(s => s.IdServicio == idServicio, cancellationToken);

    public async Task AgregarAsync(Servicio servicio, CancellationToken cancellationToken = default)
        => await _db.Servicios.AddAsync(servicio, cancellationToken);

    public void Eliminar(Servicio servicio) => _db.Servicios.Remove(servicio);
}
