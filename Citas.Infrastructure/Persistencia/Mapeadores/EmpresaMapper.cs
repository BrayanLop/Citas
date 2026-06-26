using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Models;

namespace Citas.Infrastructure.Persistencia.Mapeadores;

// Traduce entre la entidad de dominio Empresa y su modelo de tabla.
internal static class EmpresaMapper
{
    public static Empresa ToDomain(EmpresaDbModel db)
        => Empresa.Rehidratar(db.Id, db.Nombre, db.Activo);

    public static EmpresaDbModel ToDbModel(Empresa e)
        => new()
        {
            Id = e.Id,
            Nombre = e.Nombre,
            Activo = e.Activo,
        };
}
