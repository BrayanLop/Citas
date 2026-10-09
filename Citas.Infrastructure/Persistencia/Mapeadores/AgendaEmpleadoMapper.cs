using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Models;

namespace Citas.Infrastructure.Persistencia.Mapeadores;

internal static class AgendaEmpleadoMapper
{
    public static AgendaEmpleado ToDomain(int idEmpleado, IEnumerable<AgendaEmpleadoDbModel> filas)
        => AgendaEmpleado.Rehidratar(
            idEmpleado,
            filas.Select(f => new FranjaHoraria((DayOfWeek)f.DiaSemana, f.HoraInicio, f.HoraFin)));

    public static IEnumerable<AgendaEmpleadoDbModel> ToDbModels(AgendaEmpleado agenda)
        => agenda.Franjas.Select(f => new AgendaEmpleadoDbModel
        {
            IdUsuario = agenda.IdEmpleado,
            DiaSemana = (int)f.Dia,
            HoraInicio = f.Inicio,
            HoraFin = f.Fin,
            // TenantId lo sella el DbContext al guardar.
        });

    public static BloqueoAgenda ToDomain(BloqueoAgendaDbModel db)
        => BloqueoAgenda.Rehidratar(db.IdBloqueo, db.IdEmpleado, db.Inicio, db.Fin, db.Motivo);

    public static BloqueoAgendaDbModel ToDbModel(BloqueoAgenda b)
        => new()
        {
            IdBloqueo = b.IdBloqueo,
            IdEmpleado = b.IdEmpleado,
            Inicio = b.Inicio,
            Fin = b.Fin,
            Motivo = b.Motivo,
        };
}
