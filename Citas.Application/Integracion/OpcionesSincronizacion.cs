namespace Citas.Application.Integracion;

/// <summary>Parámetros de la sincronización con Natural (NaturalApi:* en la configuración).</summary>
public sealed class OpcionesSincronizacion
{
    public const int TiempoEstimadoPorDefectoInicial = 30;

    /// <summary>Duración (minutos) que recibe un servicio nuevo de Natural hasta que el personal la ajuste.</summary>
    public int TiempoEstimadoPorDefecto { get; init; } = TiempoEstimadoPorDefectoInicial;
}
