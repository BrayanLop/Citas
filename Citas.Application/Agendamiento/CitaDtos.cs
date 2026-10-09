namespace Citas.Application.Agendamiento;

// Convención de fechas de la API de citas (ver README, "Fechas y zona horaria"):
//  - fechaCita + horaEstimadaCita son fecha y hora LOCALES del negocio (Agenda:ZonaHoraria).
//    fechaCita se envía como "YYYY-MM-DD" o "YYYY-MM-DDT00:00:00" (solo cuenta la fecha);
//    horaEstimadaCita como "HH:mm:ss".
//  - En la respuesta, además, inicio/fin son el instante con el offset del negocio.

public class CreateCitaDto
{
    /// <summary>El cliente lo puede enviar en 0: se usa su propio usuario.</summary>
    public int IdCliente { get; set; }

    public int IdEmpleado { get; set; }
    public DateTime FechaCita { get; set; }
    public TimeSpan HoraEstimadaCita { get; set; }
    public string? Observaciones { get; set; }

    /// <summary>Servicios de la cita (ids de Citas, agendables y del tenant). Obligatorio: al menos uno.</summary>
    public List<int>? IdServicios { get; set; }
}

/// <summary>
/// Reprogramación (PUT /Citas/{id}). Todo es opcional: lo que no venga se conserva.
/// Si viene idServicios, reemplaza los servicios (nueva foto de valor y duración).
/// El estado se cambia con PATCH, no aquí.
/// </summary>
public class UpdateCitaDto
{
    public DateTime? FechaCita { get; set; }
    public TimeSpan? HoraEstimadaCita { get; set; }
    public int? IdEmpleado { get; set; }
    public List<int>? IdServicios { get; set; }
    public string? Observaciones { get; set; }
}

public class PartialUpdateCitaDto
{
    public string? Estado { get; set; }
}

public class DetalleCitaDto
{
    public int IdServicio { get; set; }
    public string? NombreServicio { get; set; }
    public decimal ValorServicio { get; set; }
    public int TiempoEstimado { get; set; }
}

public class CitaResponseDto
{
    public int IdCita { get; set; }
    public int IdCliente { get; set; }
    public int IdEmpleado { get; set; }
    public string Estado { get; set; } = string.Empty;

    /// <summary>Fecha local del negocio, "YYYY-MM-DDT00:00:00".</summary>
    public DateTime FechaCita { get; set; }

    /// <summary>Hora local de inicio, "HH:mm:ss".</summary>
    public TimeSpan HoraEstimadaCita { get; set; }

    /// <summary>Hora local de fin, "HH:mm:ss" (inicio + duración total).</summary>
    public TimeSpan HoraEstimadaFin { get; set; }

    /// <summary>Instante de inicio con el offset del negocio, p. ej. "2026-10-20T09:00:00-05:00".</summary>
    public DateTimeOffset Inicio { get; set; }

    public DateTimeOffset Fin { get; set; }

    public string? Observaciones { get; set; }
    public string? NombreCliente { get; set; }
    public string? NombreEmpleado { get; set; }
    public List<DetalleCitaDto> Servicios { get; set; } = [];

    /// <summary>Suma de las duraciones de los servicios (minutos).</summary>
    public int DuracionTotal { get; set; }

    public decimal ValorTotal { get; set; }
}
