using Citas.Data;
using Citas.Domain.Agendamiento;
using Citas.Infrastructure.Persistencia;
using Citas.Infrastructure.Persistencia.Mapeadores;
using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Agendamiento;

public class AgendaEmpleadoRepository : IAgendaEmpleadoRepository
{
    // Clase (primer entero) de los advisory locks de PostgreSQL que usa Citas para la
    // agenda; el segundo entero es el id del empleado (único en toda la base).
    private const int ClaseBloqueoAgenda = 7301;

    private readonly CitasContext _db;

    public AgendaEmpleadoRepository(CitasContext db) => _db = db;

    public async Task<AgendaEmpleado> ObtenerHorarioAsync(int idEmpleado, CancellationToken cancellationToken = default)
    {
        var filas = await _db.AgendaEmpleados
            .AsNoTracking()
            .Where(a => a.IdUsuario == idEmpleado)
            .ToListAsync(cancellationToken);

        return AgendaEmpleadoMapper.ToDomain(idEmpleado, filas);
    }

    public async Task ReemplazarHorarioAsync(AgendaEmpleado agenda, CancellationToken cancellationToken = default)
    {
        var actuales = await _db.AgendaEmpleados
            .Where(a => a.IdUsuario == agenda.IdEmpleado)
            .ToListAsync(cancellationToken);

        _db.AgendaEmpleados.RemoveRange(actuales);
        await _db.AgendaEmpleados.AddRangeAsync(AgendaEmpleadoMapper.ToDbModels(agenda), cancellationToken);
    }

    public async Task<IReadOnlyList<BloqueoAgenda>> ListarBloqueosAsync(
        int idEmpleado,
        DateTime desdeUtc,
        DateTime hastaUtc,
        CancellationToken cancellationToken = default)
    {
        var filas = await _db.BloqueosAgenda
            .AsNoTracking()
            .Where(b => b.IdEmpleado == idEmpleado && b.Inicio < hastaUtc && desdeUtc < b.Fin)
            .OrderBy(b => b.Inicio)
            .ToListAsync(cancellationToken);

        return filas.Select(AgendaEmpleadoMapper.ToDomain).ToList();
    }

    public async Task<BloqueoAgenda?> ObtenerBloqueoAsync(int idEmpleado, int idBloqueo, CancellationToken cancellationToken = default)
    {
        var fila = await _db.BloqueosAgenda
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.IdBloqueo == idBloqueo && b.IdEmpleado == idEmpleado, cancellationToken);

        return fila is null ? null : AgendaEmpleadoMapper.ToDomain(fila);
    }

    public async Task AgregarBloqueoAsync(BloqueoAgenda bloqueo, CancellationToken cancellationToken = default)
    {
        var fila = AgendaEmpleadoMapper.ToDbModel(bloqueo);
        await _db.BloqueosAgenda.AddAsync(fila, cancellationToken);
        _db.SincronizarIdTrasGuardar(() => bloqueo.EstablecerIdentidad(fila.IdBloqueo));
    }

    public void EliminarBloqueo(BloqueoAgenda bloqueo)
        => _db.BloqueosAgenda.Remove(new BloqueoAgendaDbModel { IdBloqueo = bloqueo.IdBloqueo });

    public async Task BloquearAgendaAsync(int idEmpleado, CancellationToken cancellationToken = default)
    {
        if (_db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("BloquearAgendaAsync requiere una transacción abierta.");

        // Se libera solo al confirmar o deshacer la transacción.
        await _db.Database.ExecuteSqlAsync(
            $"SELECT pg_advisory_xact_lock({ClaseBloqueoAgenda}, {idEmpleado})",
            cancellationToken);
    }
}
