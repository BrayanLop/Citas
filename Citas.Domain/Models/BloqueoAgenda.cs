using Citas.Domain.Common;

namespace Citas.Models;

// Ausencia o bloqueo puntual en la agenda de un empleado (vacaciones, cita médica, una
// tarde libre...). Durante [Inicio, Fin) no se le pueden agendar citas. Instantes en UTC.
public class BloqueoAgenda
{
    public const int LongitudMaximaMotivo = 200;

    private BloqueoAgenda() { }

    public int IdBloqueo { get; private set; }
    public int IdEmpleado { get; private set; }

    /// <summary>Instante de inicio (UTC).</summary>
    public DateTime Inicio { get; private set; }

    /// <summary>Instante de fin (UTC), excluido.</summary>
    public DateTime Fin { get; private set; }

    public string? Motivo { get; private set; }

    public static BloqueoAgenda Crear(int idEmpleado, DateTime inicioUtc, DateTime finUtc, string? motivo)
    {
        if (idEmpleado <= 0)
            throw new DomainException("El bloqueo requiere un empleado válido.");
        if (inicioUtc.Kind != DateTimeKind.Utc || finUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("Los instantes del bloqueo deben venir en UTC.");
        if (finUtc <= inicioUtc)
            throw new DomainException("El inicio del bloqueo debe ser anterior a su fin.");

        motivo = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        if (motivo is { Length: > LongitudMaximaMotivo })
            throw new DomainException($"El motivo no puede superar {LongitudMaximaMotivo} caracteres.");

        return new BloqueoAgenda { IdEmpleado = idEmpleado, Inicio = inicioUtc, Fin = finUtc, Motivo = motivo };
    }

    public static BloqueoAgenda Rehidratar(int idBloqueo, int idEmpleado, DateTime inicio, DateTime fin, string? motivo)
        => new()
        {
            IdBloqueo = idBloqueo,
            IdEmpleado = idEmpleado,
            Inicio = DateTime.SpecifyKind(inicio, DateTimeKind.Utc),
            Fin = DateTime.SpecifyKind(fin, DateTimeKind.Utc),
            Motivo = motivo,
        };

    // La persistencia asigna el id real una sola vez, tras el primer guardado.
    public void EstablecerIdentidad(int id)
    {
        if (IdBloqueo == 0) IdBloqueo = id;
    }
}
