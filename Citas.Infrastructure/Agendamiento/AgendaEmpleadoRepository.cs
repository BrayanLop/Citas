using Citas.Data;
using Citas.Domain.Agendamiento;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Agendamiento;

public class AgendaEmpleadoRepository : IAgendaEmpleadoRepository
{
    private readonly CitasContext _db;

    public AgendaEmpleadoRepository(CitasContext db) => _db = db;

    public async Task<AgendaEmpleado?> ObtenerPorIdAsync(int idHorario, CancellationToken cancellationToken = default)
        => await _db.AgendaEmpleados.FirstOrDefaultAsync(a => a.IdHorario == idHorario, cancellationToken);
}
