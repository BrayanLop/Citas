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
            db.Empleado is null ? null : UsuarioMapper.ToDomain(db.Empleado),
            db.DetalleCitas.Select(d => DetalleCita.Rehidratar(
                d.IdDetalle, d.IdServicio, d.ValorServicio, d.TiempoEstimado, d.Servicio?.NombreServicio)));

    // Los detalles solo viajan al insertar la cita (son inmutables después); al actualizar
    // se mandan solo las columnas de la cita para no tocar sus líneas.
    public static CitaDbModel ToDbModel(Cita c, bool incluirDetalles = false)
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
            // Sin navegaciones a Usuario a propósito: al guardar no viajan cliente ni empleado.
            DetalleCitas = incluirDetalles
                ? c.Detalles.Select(d => new DetalleCitaDbModel
                {
                    IdServicio = d.IdServicio,
                    ValorServicio = d.ValorServicio,
                    TiempoEstimado = d.TiempoEstimado,
                    // TenantId lo sella el DbContext al guardar.
                }).ToList()
                : [],
        };
}
