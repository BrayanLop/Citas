namespace Citas.Application.Agendamiento;

// Horas en hora local del negocio, formato "HH:mm:ss" (TimeSpan), igual que horaEstimadaCita.
// Día de la semana como número: 0 = domingo, 1 = lunes ... 6 = sábado (igual que Date.getDay() en JS).

public class FranjaHorariaDto
{
    public int Dia { get; set; }
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
}

/// <summary>Cuerpo de PUT /AgendaEmpleado/{usuarioId}: el horario semanal completo.</summary>
public class ActualizarHorarioDto
{
    public List<FranjaHorariaDto> Franjas { get; set; } = [];
}

public class HorarioEmpleadoDto
{
    public int IdEmpleado { get; set; }
    public string? NombreEmpleado { get; set; }

    /// <summary>Zona horaria del negocio en la que están expresadas las horas.</summary>
    public string ZonaHoraria { get; set; } = string.Empty;

    public List<FranjaHorariaDto> Franjas { get; set; } = [];
}

/// <summary>
/// Cuerpo de POST /AgendaEmpleado/{usuarioId}/bloqueos. Fechas ISO 8601: sin offset se
/// toman como hora local del negocio ("2026-10-20T08:00:00"); con offset o 'Z' se respeta
/// el instante ("2026-10-20T08:00:00-05:00").
/// </summary>
public class CrearBloqueoDto
{
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public string? Motivo { get; set; }
}

public class BloqueoDto
{
    public int IdBloqueo { get; set; }
    public int IdEmpleado { get; set; }

    /// <summary>Instante con el offset del negocio, p. ej. "2026-10-20T08:00:00-05:00".</summary>
    public DateTimeOffset Inicio { get; set; }

    public DateTimeOffset Fin { get; set; }
    public string? Motivo { get; set; }
}

public class DisponibilidadDto
{
    public int IdEmpleado { get; set; }

    /// <summary>Fecha local consultada ("YYYY-MM-DD").</summary>
    public DateOnly Fecha { get; set; }

    /// <summary>Minutos (suma de los servicios pedidos).</summary>
    public int DuracionTotal { get; set; }

    public int IntervaloMinutos { get; set; }
    public string ZonaHoraria { get; set; } = string.Empty;

    /// <summary>Horas de inicio libres, hora local "HH:mm:ss", ordenadas.</summary>
    public List<TimeSpan> Horas { get; set; } = [];
}

public class DiasDisponiblesDto
{
    public int IdEmpleado { get; set; }
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public int DuracionTotal { get; set; }

    /// <summary>Días ("YYYY-MM-DD") con al menos una hora de inicio libre.</summary>
    public List<DateOnly> Dias { get; set; } = [];
}
