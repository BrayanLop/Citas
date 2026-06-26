using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Models;

namespace Citas.Infrastructure.Persistencia.Mapeadores;

internal static class ServicioMapper
{
    public static Servicio ToDomain(ServicioDbModel db)
        => Servicio.Rehidratar(db.IdServicio, db.NombreServicio, db.Valor, db.TiempoEstimado);

    public static ServicioDbModel ToDbModel(Servicio s)
        => new()
        {
            IdServicio = s.IdServicio,
            NombreServicio = s.NombreServicio,
            Valor = s.Valor,
            TiempoEstimado = s.TiempoEstimado,
            // TenantId lo sella el DbContext al guardar.
        };
}
