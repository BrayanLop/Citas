using Citas.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

[ApiController]
[Route("health")]
[AllowAnonymous]
public class HealthController : ControllerBase
{
    private readonly CitasContext _db;
    private readonly IConfiguration _config;

    public HealthController(CitasContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet]
    public async Task<ActionResult<HealthCheckResponse>> Health()
    {
        var response = new HealthCheckResponse
        {
            Status = "ok",
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown"
        };

        // Verificar conexión a BD
        try
        {
            await _db.Database.CanConnectAsync();
            response.DatabaseStatus = "connected";
        }
        catch (Exception ex)
        {
            response.DatabaseStatus = $"failed: {ex.Message}";
            response.Status = "degraded";
        }

        // Verificar variables de entorno
        response.HasJwtKey = !string.IsNullOrEmpty(_config["Jwt:Key"]);
        response.HasConnectionString = !string.IsNullOrEmpty(_config["ConnectionStrings:DefaultConnection"]);

        return Ok(response);
    }
}

public class HealthCheckResponse
{
    public string Status { get; set; } = "ok";
    public DateTime Timestamp { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string DatabaseStatus { get; set; } = "unknown";
    public bool HasJwtKey { get; set; }
    public bool HasConnectionString { get; set; }
}
