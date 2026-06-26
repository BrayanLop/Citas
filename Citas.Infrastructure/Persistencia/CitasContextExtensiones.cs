using Citas.Data;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Persistencia;

internal static class CitasContextExtensiones
{
    // Al insertar, EF asigna el id autogenerado al DbModel, no a la entidad de dominio.
    // Esto engancha un handler de un solo uso que, en cuanto se guardan los cambios,
    // copia ese id de vuelta al dominio (para que la app pueda devolverlo o reusarlo).
    public static void SincronizarIdTrasGuardar(this CitasContext db, Action copiarId)
    {
        void Handler(object? sender, SavedChangesEventArgs e)
        {
            copiarId();
            db.SavedChanges -= Handler;
        }
        db.SavedChanges += Handler;
    }
}
