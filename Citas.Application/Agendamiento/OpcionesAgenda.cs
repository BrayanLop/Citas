namespace Citas.Application.Agendamiento;

/// <summary>
/// Configuración de la agenda (sección <c>Agenda</c>):
///  - ZonaHoraria (Agenda__ZonaHoraria): id IANA de la zona del negocio. Por defecto America/Bogota.
///  - IntervaloMinutos (Agenda__IntervaloMinutos): cada cuánto se ofrecen horas de inicio. Por defecto 15.
///  - AnticipacionMinimaMinutos (Agenda__AnticipacionMinimaMinutos): margen mínimo entre "ahora"
///    y el inicio de una cita nueva o reprogramada. Por defecto 0.
/// </summary>
public sealed class OpcionesAgenda
{
    public const string ZonaHorariaPorDefecto = "America/Bogota";
    public const int IntervaloMinutosPorDefecto = 15;

    public required TimeZoneInfo ZonaHoraria { get; init; }
    public int IntervaloMinutos { get; init; } = IntervaloMinutosPorDefecto;
    public int AnticipacionMinimaMinutos { get; init; }

    /// <summary>
    /// Busca la zona por id (IANA o de Windows). Si es la zona por defecto y el sistema no
    /// la tiene (p. ej. una imagen de contenedor sin tzdata), usa UTC-5 fijo: Colombia no
    /// tiene horario de verano, así que es equivalente. Cualquier otra zona desconocida es
    /// un error de configuración.
    /// </summary>
    public static TimeZoneInfo ResolverZona(string? id)
    {
        id = string.IsNullOrWhiteSpace(id) ? ZonaHorariaPorDefecto : id.Trim();
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            if (id == ZonaHorariaPorDefecto)
                return TimeZoneInfo.CreateCustomTimeZone("UTC-05:00", TimeSpan.FromHours(-5), "UTC-05:00", "UTC-05:00");
            throw new InvalidOperationException($"Agenda__ZonaHoraria: la zona horaria '{id}' no existe en este sistema.", e);
        }
    }
}
