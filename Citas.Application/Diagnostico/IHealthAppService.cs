namespace Citas.Application.Diagnostico;

/// <summary>
/// Estado del servicio. A propósito no expone detalles de configuración ni mensajes de
/// error (es un endpoint anónimo).
/// </summary>
public class HealthStatusDto
{
    /// <summary>"ok" o "degraded" (sin conexión a la base de datos).</summary>
    public string Status { get; set; } = "ok";
    public DateTime Timestamp { get; set; }
}

/// <summary>Diagnóstico del servicio: conectividad a BD.</summary>
public interface IHealthAppService
{
    Task<HealthStatusDto> ComprobarAsync(CancellationToken cancellationToken = default);
}
