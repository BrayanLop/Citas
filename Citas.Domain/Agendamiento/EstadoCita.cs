namespace Citas.Domain.Agendamiento;

/// <summary>
/// Estados válidos de una <see cref="Citas.Models.Cita"/> y las transiciones
/// permitidas entre ellos. Se persiste como texto en la columna 'Estado', pero
/// las reglas de transición viven aquí, en el dominio.
/// </summary>
public static class EstadoCita
{
    public const string Pendiente = "Pendiente";
    public const string Confirmada = "Confirmada";
    public const string Atendida = "Atendida";
    public const string Cancelada = "Cancelada";

    public static readonly IReadOnlyList<string> Todos =
        [Pendiente, Confirmada, Atendida, Cancelada];

    // Máquina de estados: desde cada estado, a cuáles se puede pasar.
    private static readonly IReadOnlyDictionary<string, string[]> Transiciones =
        new Dictionary<string, string[]>
        {
            [Pendiente] = [Confirmada, Cancelada],
            [Confirmada] = [Atendida, Cancelada],
            [Atendida] = [],
            [Cancelada] = [],
        };

    /// <summary>
    /// Estados que ocupan la agenda del empleado. Las citas Atendidas y Canceladas ya no
    /// cuentan para cruces ni disponibilidad. La restricción de exclusión de la tabla Citas
    /// (migración InitialCreate) usa esta misma lista: si cambia, hay que cambiarla allí.
    /// </summary>
    public static readonly IReadOnlyList<string> Activos = [Pendiente, Confirmada];

    public static bool EsActivo(string? estado) =>
        estado is not null && Activos.Contains(estado);

    public static bool EsValido(string? estado) =>
        estado is not null && Todos.Contains(estado);

    public static bool PuedeTransicionar(string desde, string hacia) =>
        Transiciones.TryGetValue(desde, out var permitidos) && permitidos.Contains(hacia);
}
