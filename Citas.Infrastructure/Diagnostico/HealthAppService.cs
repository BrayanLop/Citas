using Citas.Application.Diagnostico;
using Citas.Data;

namespace Citas.Infrastructure.Diagnostico;

public class HealthAppService : IHealthAppService
{
    private readonly CitasContext _db;

    public HealthAppService(CitasContext db) => _db = db;

    public async Task<HealthStatusDto> ComprobarAsync(CancellationToken cancellationToken = default)
    {
        bool conectada;
        try
        {
            conectada = await _db.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            // El detalle del error no se publica (endpoint anónimo); queda en los logs de EF.
            conectada = false;
        }

        return new HealthStatusDto
        {
            Status = conectada ? "ok" : "degraded",
            Timestamp = DateTime.UtcNow,
        };
    }
}
