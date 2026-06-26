using Citas.Data;
using Citas.Domain.Servicios;
using Citas.Infrastructure.Persistencia;
using Citas.Infrastructure.Persistencia.Mapeadores;
using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Servicios;

public class ServicioRepository : IServicioRepository
{
    private readonly CitasContext _db;

    public ServicioRepository(CitasContext db) => _db = db;

    public async Task<IReadOnlyList<Servicio>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var filas = await _db.Servicios
            .AsNoTracking()
            .OrderBy(s => s.NombreServicio)
            .ToListAsync(cancellationToken);

        return filas.Select(ServicioMapper.ToDomain).ToList();
    }

    public async Task<Servicio?> ObtenerPorIdAsync(int idServicio, CancellationToken cancellationToken = default)
    {
        var fila = await _db.Servicios
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.IdServicio == idServicio, cancellationToken);

        return fila is null ? null : ServicioMapper.ToDomain(fila);
    }

    public async Task AgregarAsync(Servicio servicio, CancellationToken cancellationToken = default)
    {
        var fila = ServicioMapper.ToDbModel(servicio);
        await _db.Servicios.AddAsync(fila, cancellationToken);
        _db.SincronizarIdTrasGuardar(() => servicio.EstablecerIdentidad(fila.IdServicio));
    }

    public void Actualizar(Servicio servicio) => _db.Servicios.Update(ServicioMapper.ToDbModel(servicio));

    public void Eliminar(Servicio servicio)
        => _db.Servicios.Remove(new ServicioDbModel { IdServicio = servicio.IdServicio });
}
