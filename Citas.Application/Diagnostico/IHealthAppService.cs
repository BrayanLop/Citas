namespace Citas.Application.Diagnostico;

public class HealthStatusDto
{
    public string Status { get; set; } = "ok";
    public DateTime Timestamp { get; set; }
    public string Environment { get; set; } = string.Empty;
    public string DatabaseStatus { get; set; } = "unknown";
    public bool HasJwtKey { get; set; }
    public bool HasConnectionString { get; set; }
}

/// <summary>Diagnóstico del servicio: conectividad a BD y presencia de configuración.</summary>
public interface IHealthAppService
{
    Task<HealthStatusDto> ComprobarAsync(CancellationToken cancellationToken = default);
}
