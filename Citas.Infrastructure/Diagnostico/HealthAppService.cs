using Citas.Application.Diagnostico;
using Citas.Data;
using Microsoft.Extensions.Configuration;

namespace Citas.Infrastructure.Diagnostico;

public class HealthAppService : IHealthAppService
{
    private readonly CitasContext _db;
    private readonly IConfiguration _config;

    public HealthAppService(CitasContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<HealthStatusDto> ComprobarAsync(CancellationToken cancellationToken = default)
    {
        var status = new HealthStatusDto
        {
            Status = "ok",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown",
            HasJwtKey = !string.IsNullOrEmpty(_config["Jwt:Key"]),
            HasConnectionString = !string.IsNullOrEmpty(_config["ConnectionStrings:DefaultConnection"]),
        };

        try
        {
            await _db.Database.CanConnectAsync(cancellationToken);
            status.DatabaseStatus = "connected";
        }
        catch (Exception ex)
        {
            status.DatabaseStatus = $"failed: {ex.Message}";
            status.Status = "degraded";
        }

        return status;
    }
}
