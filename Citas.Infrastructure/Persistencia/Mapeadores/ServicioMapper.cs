using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Models;

namespace Citas.Infrastructure.Persistencia.Mapeadores;

internal static class ServicioMapper
{
    public static Servicio ToDomain(ServicioDbModel db)
        => Servicio.Rehidratar(
            db.IdServicio,
            db.ServicioNaturalId,
            db.NombreServicio,
            db.Valor,
            db.TiempoEstimado,
            db.Activo,
            db.DisponibleEnNatural);

    public static ServicioDbModel ToDbModel(Servicio s)
        => new()
        {
            IdServicio = s.IdServicio,
            ServicioNaturalId = s.ServicioNaturalId,
            NombreServicio = s.NombreServicio,
            Valor = s.Valor,
            TiempoEstimado = s.TiempoEstimado,
            Activo = s.Activo,
            DisponibleEnNatural = s.DisponibleEnNatural,
            // TenantId lo sella el DbContext al guardar.
        };
}
