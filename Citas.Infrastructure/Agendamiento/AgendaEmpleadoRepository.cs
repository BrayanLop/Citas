using Citas.Data;
using Citas.Domain.Agendamiento;
using Citas.Infrastructure.Persistencia.Mapeadores;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Agendamiento;

public class AgendaEmpleadoRepository : IAgendaEmpleadoRepository
{
    private readonly CitasContext _db;

    public AgendaEmpleadoRepository(CitasContext db) => _db = db;

    public async Task<AgendaEmpleado?> ObtenerPorIdAsync(int idHorario, CancellationToken cancellationToken = default)
    {
        var fila = await _db.AgendaEmpleados
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.IdHorario == idHorario, cancellationToken);

        return fila is null ? null : AgendaEmpleadoMapper.ToDomain(fila);
    }

    public void Actualizar(AgendaEmpleado agenda)
        => _db.AgendaEmpleados.Update(AgendaEmpleadoMapper.ToDbModel(agenda));
}
