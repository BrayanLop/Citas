namespace Citas.Application.Servicios;

/// <summary>
/// Lo único que se ajusta de un servicio en Citas (nombre y valor vienen de Natural).
/// Los campos omitidos (null) no cambian.
/// </summary>
public class UpdateServicioDto
{
    /// <summary>Duración en minutos (mayor que cero).</summary>
    public int? TiempoEstimado { get; set; }

    /// <summary>Disponibilidad para agendar decidida en Citas.</summary>
    public bool? Activo { get; set; }
}

public class ServicioResponseDto
{
    public int IdServicio { get; set; }
    public int ServicioNaturalId { get; set; }
    public string NombreServicio { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int TiempoEstimado { get; set; }

    /// <summary>Disponibilidad ajustada en Citas.</summary>
    public bool Activo { get; set; }

    /// <summary>Sigue en Natural y allí está disponible.</summary>
    public bool DisponibleEnNatural { get; set; }

    /// <summary>Se puede agendar (Activo y DisponibleEnNatural).</summary>
    public bool Agendable { get; set; }
}
