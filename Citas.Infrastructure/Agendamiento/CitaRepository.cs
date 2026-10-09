using Citas.Data;
using Citas.Domain.Agendamiento;
using Citas.Infrastructure.Persistencia;
using Citas.Infrastructure.Persistencia.Mapeadores;
using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Agendamiento;

// Implementa el puerto ICitaRepository sobre EF Core. Lee con AsNoTracking y devuelve
// dominio (vía mapper); escribe traduciendo el dominio de vuelta a CitaDbModel.
// El aislamiento por tenant lo aplican los filtros globales del contexto.
public class CitaRepository : ICitaRepository
{
    private readonly CitasContext _db;

    public CitaRepository(CitasContext db) => _db = db;

    public async Task<IReadOnlyList<Cita>> ListarAsync(
        int? clienteId = null,
        int? empleadoId = null,
        CancellationToken cancellationToken = default)
    {
        var query = ConDetalles();

        if (clienteId.HasValue)
            query = query.Where(c => c.IdCliente == clienteId.Value);
        if (empleadoId.HasValue)
            query = query.Where(c => c.IdEmpleado == empleadoId.Value);

        var filas = await query
            .OrderByDescending(c => c.Inicio)
            .ToListAsync(cancellationToken);

        return filas.Select(CitaMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Cita>> ListarActivasDeEmpleadoAsync(
        int idEmpleado,
        DateTime desdeUtc,
        DateTime hastaUtc,
        int? excluirIdCita = null,
        CancellationToken cancellationToken = default)
    {
        var activos = EstadoCita.Activos.ToList();
        var query = _db.Citas
            .AsNoTracking()
            .Where(c => c.IdEmpleado == idEmpleado
                && activos.Contains(c.Estado)
                && c.Inicio < hastaUtc
                && desdeUtc < c.Fin);
        if (excluirIdCita.HasValue)
            query = query.Where(c => c.IdCita != excluirIdCita.Value);

        var filas = await query.OrderBy(c => c.Inicio).ToListAsync(cancellationToken);
        return filas.Select(CitaMapper.ToDomain).ToList();
    }

    public async Task<Cita?> ObtenerPorIdAsync(int idCita, CancellationToken cancellationToken = default)
    {
        var fila = await ConDetalles().FirstOrDefaultAsync(c => c.IdCita == idCita, cancellationToken);
        return fila is null ? null : CitaMapper.ToDomain(fila);
    }

    public Task<bool> ExisteUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default)
        => _db.Usuarios.AsNoTracking().AnyAsync(u => u.Id == usuarioId, cancellationToken);

    public async Task AgregarAsync(Cita cita, CancellationToken cancellationToken = default)
    {
        var fila = CitaMapper.ToDbModel(cita, incluirDetalles: true);
        await _db.Citas.AddAsync(fila, cancellationToken);
        _db.SincronizarIdTrasGuardar(() => cita.EstablecerIdentidad(fila.IdCita));
    }

    public void Actualizar(Cita cita)
    {
        _db.Citas.Update(CitaMapper.ToDbModel(cita));

        if (!cita.DetallesReemplazados)
            return;
        foreach (var retirado in cita.DetallesRetirados)
            _db.DetalleCitas.Remove(new DetalleCitaDbModel { IdDetalle = retirado.IdDetalle });
        _db.DetalleCitas.AddRange(cita.Detalles.Select(d => CitaMapper.ToDbModel(d, cita.IdCita)));
    }

    public void Eliminar(Cita cita) => _db.Citas.Remove(new CitaDbModel { IdCita = cita.IdCita });

    private IQueryable<CitaDbModel> ConDetalles()
        => _db.Citas
            .AsNoTracking()
            .Include(c => c.Cliente)
            .Include(c => c.Empleado)
            .Include(c => c.DetalleCitas)
                .ThenInclude(d => d.Servicio);
}
