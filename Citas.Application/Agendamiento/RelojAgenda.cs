using Citas.Domain.Common;

namespace Citas.Application.Agendamiento;

/// <summary>
/// Conversión entre instantes UTC (lo que se guarda) y la hora local del negocio (lo que
/// ve y envía la API), según <see cref="OpcionesAgenda.ZonaHoraria"/>. Convención:
///  - En base de datos, Inicio/Fin de citas y bloqueos son <c>timestamptz</c> en UTC.
///  - La API recibe fecha y hora locales del negocio (sin offset) o un instante con offset
///    / 'Z', y responde con la fecha/hora local y el instante con el offset del negocio.
/// </summary>
public sealed class RelojAgenda
{
    private readonly TimeProvider _tiempo;

    public RelojAgenda(OpcionesAgenda opciones, TimeProvider tiempo)
    {
        Zona = opciones.ZonaHoraria;
        _tiempo = tiempo;
    }

    public TimeZoneInfo Zona { get; }

    public DateTime AhoraUtc => _tiempo.GetUtcNow().UtcDateTime;

    /// <summary>Fecha de hoy en la zona del negocio.</summary>
    public DateOnly Hoy => DateOnly.FromDateTime(ALocal(AhoraUtc));

    /// <summary>Hora local del negocio (Kind=Unspecified) de un instante UTC.</summary>
    public DateTime ALocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);

    /// <summary>El instante expresado en hora local del negocio, con su offset (p. ej. -05:00).</summary>
    public DateTimeOffset ConOffset(DateTime utc)
    {
        utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return new DateTimeOffset(ALocal(utc), Zona.GetUtcOffset(utc));
    }

    public DateTime AUtc(DateOnly fecha, TimeOnly hora) => AUtc(fecha.ToDateTime(hora));

    /// <summary>
    /// Instante UTC de un DateTime recibido por la API: sin zona (Unspecified) se interpreta
    /// como hora local del negocio; con 'Z' u offset se respeta el instante.
    /// </summary>
    public DateTime AUtc(DateTime valor)
    {
        switch (valor.Kind)
        {
            case DateTimeKind.Utc:
                return valor;
            case DateTimeKind.Local:
                return valor.ToUniversalTime();
            default:
                if (Zona.IsInvalidTime(valor))
                    throw new DomainException($"La hora {valor:yyyy-MM-dd HH:mm} no existe en la zona horaria del negocio.");
                return TimeZoneInfo.ConvertTimeToUtc(valor, Zona);
        }
    }

    /// <summary>Rango UTC [desde, hasta) que ocupa un día local del negocio.</summary>
    public (DateTime Desde, DateTime Hasta) DiaEnUtc(DateOnly fecha)
        => (AUtc(fecha, TimeOnly.MinValue), AUtc(fecha.AddDays(1), TimeOnly.MinValue));
}
