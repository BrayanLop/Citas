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

    public static bool EsValido(string? estado) =>
        estado is not null && Todos.Contains(estado);

    public static bool PuedeTransicionar(string desde, string hacia) =>
        Transiciones.TryGetValue(desde, out var permitidos) && permitidos.Contains(hacia);
}
