using Citas.Data;
using Citas.Domain.Agendamiento;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Agendamiento;

/// <summary>
/// Implementación de <see cref="ICitaRepository"/> sobre EF Core. El aislamiento por
/// tenant lo aplican los filtros globales del <see cref="CitasContext"/>.
/// </summary>
public class CitaRepository : ICitaRepository
{
    private readonly CitasContext _db;

    public CitaRepository(CitasContext db) => _db = db;

    public async Task<IReadOnlyList<Cita>> ListarAsync(
        int? clienteId = null,
        int? empleadoId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Empleado)
            .AsQueryable();

        if (clienteId.HasValue)
            query = query.Where(c => c.IdCliente == clienteId.Value);
        if (empleadoId.HasValue)
            query = query.Where(c => c.IdEmpleado == empleadoId.Value);

        return await query
            .OrderByDescending(c => c.FechaCita)
            .ThenByDescending(c => c.HoraEstimadaCita)
            .ToListAsync(cancellationToken);
    }

    public async Task<Cita?> ObtenerPorIdAsync(int idCita, CancellationToken cancellationToken = default)
        => await _db.Citas
            .Include(c => c.Cliente)
            .Include(c => c.Empleado)
            .FirstOrDefaultAsync(c => c.IdCita == idCita, cancellationToken);

    public async Task<bool> ExisteUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default)
        => await _db.Usuarios.AnyAsync(u => u.Id == usuarioId, cancellationToken);

    public async Task AgregarAsync(Cita cita, CancellationToken cancellationToken = default)
        => await _db.Citas.AddAsync(cita, cancellationToken);

    public void Eliminar(Cita cita) => _db.Citas.Remove(cita);
}
