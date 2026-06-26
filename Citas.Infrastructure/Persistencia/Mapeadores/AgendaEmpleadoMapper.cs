using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Models;

namespace Citas.Infrastructure.Persistencia.Mapeadores;

internal static class AgendaEmpleadoMapper
{
    public static AgendaEmpleado ToDomain(AgendaEmpleadoDbModel db)
        => AgendaEmpleado.Rehidratar(
            db.IdHorario, db.IdUsuario, db.FechaReservada,
            db.HoraEstimadaCita, db.HoraEstimadaFin, db.EstadoHorario);

    public static AgendaEmpleadoDbModel ToDbModel(AgendaEmpleado a)
        => new()
        {
            IdHorario = a.IdHorario,
            IdUsuario = a.IdUsuario,
            FechaReservada = a.FechaReservada,
            HoraEstimadaCita = a.HoraEstimadaCita,
            HoraEstimadaFin = a.HoraEstimadaFin,
            EstadoHorario = a.EstadoHorario,
        };
}
