using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Models;

namespace Citas.Infrastructure.Persistencia.Mapeadores;

internal static class CitaMapper
{
    public static Cita ToDomain(CitaDbModel db)
        => Cita.Rehidratar(
            db.IdCita,
            db.IdCliente,
            db.IdEmpleado,
            db.Estado,
            db.FechaCita,
            db.HoraEstimadaCita,
            db.HoraEstimadaFin,
            db.Observaciones,
            db.Cliente is null ? null : UsuarioMapper.ToDomain(db.Cliente),
            db.Empleado is null ? null : UsuarioMapper.ToDomain(db.Empleado));

    public static CitaDbModel ToDbModel(Cita c)
        => new()
        {
            IdCita = c.IdCita,
            IdCliente = c.IdCliente,
            IdEmpleado = c.IdEmpleado,
            Estado = c.Estado,
            FechaCita = c.FechaCita,
            HoraEstimadaCita = c.HoraEstimadaCita,
            HoraEstimadaFin = c.HoraEstimadaFin,
            Observaciones = c.Observaciones,
            // Sin navegaciones a propósito: al guardar solo viajan las columnas de la cita.
        };
}
