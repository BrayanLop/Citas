using Citas.Domain.Common;

namespace Citas.Models;

// Servicio del catálogo de una empresa. El catálogo vive en Natural: Citas no crea ni
// borra servicios, los sincroniza (ServicioNaturalId es la clave de vinculación, única por
// tenant). Nombre y Valor vienen de Natural; TiempoEstimado y Activo son propios de Citas.
//
// Agendable = DisponibleEnNatural && Activo:
//  - DisponibleEnNatural lo fija la sincronización (sigue en Natural y con disponible=true).
//  - Activo lo ajusta el personal en Citas; la sincronización nunca lo toca.
public class Servicio
{
    private const int LongitudMaximaNombre = 150;

    private Servicio() { }

    public int IdServicio { get; private set; }
    public int ServicioNaturalId { get; private set; }
    public string NombreServicio { get; private set; } = string.Empty;
    public decimal Valor { get; private set; }

    /// <summary>Duración en minutos (propia de Citas).</summary>
    public int TiempoEstimado { get; private set; }

    /// <summary>Disponibilidad para agendar decidida en Citas por el personal.</summary>
    public bool Activo { get; private set; } = true;

    /// <summary>Sigue en el catálogo de Natural y allí está disponible.</summary>
    public bool DisponibleEnNatural { get; private set; } = true;

    public bool EsAgendable => Activo && DisponibleEnNatural;

    // Servicio nuevo que llega de Natural en una sincronización.
    public static Servicio CrearDesdeNatural(
        int servicioNaturalId,
        string? nombreServicio,
        decimal valor,
        bool disponibleEnNatural,
        int tiempoEstimado)
    {
        if (servicioNaturalId <= 0)
            throw new DomainException("El servicio de Natural no es válido.");
        ValidarTiempo(tiempoEstimado);

        var servicio = new Servicio
        {
            ServicioNaturalId = servicioNaturalId,
            TiempoEstimado = tiempoEstimado,
            Activo = true,
        };
        servicio.SincronizarDesdeNatural(nombreServicio, valor, disponibleEnNatural);
        return servicio;
    }

    public static Servicio Rehidratar(
        int idServicio,
        int servicioNaturalId,
        string nombreServicio,
        decimal valor,
        int tiempoEstimado,
        bool activo,
        bool disponibleEnNatural)
        => new()
        {
            IdServicio = idServicio,
            ServicioNaturalId = servicioNaturalId,
            NombreServicio = nombreServicio,
            Valor = valor,
            TiempoEstimado = tiempoEstimado,
            Activo = activo,
            DisponibleEnNatural = disponibleEnNatural,
        };

    /// <summary>
    /// Copia lo que manda Natural (nombre, valor y disponibilidad). No toca TiempoEstimado
    /// ni Activo. Devuelve true si algo cambió.
    /// </summary>
    public bool SincronizarDesdeNatural(string? nombreServicio, decimal valor, bool disponibleEnNatural)
    {
        var nombre = string.IsNullOrWhiteSpace(nombreServicio)
            ? $"Servicio {ServicioNaturalId}"
            : nombreServicio.Trim();
        if (nombre.Length > LongitudMaximaNombre)
            nombre = nombre[..LongitudMaximaNombre];
        if (valor < 0)
            throw new DomainException($"El servicio '{nombre}' de Natural tiene un valor negativo.");

        var cambio = nombre != NombreServicio || valor != Valor || disponibleEnNatural != DisponibleEnNatural;
        NombreServicio = nombre;
        Valor = valor;
        DisponibleEnNatural = disponibleEnNatural;
        return cambio;
    }

    /// <summary>
    /// El servicio ya no viene de Natural: deja de ser agendable, pero no se borra (puede
    /// tener citas históricas). Devuelve true si estaba disponible.
    /// </summary>
    public bool RetirarDeNatural()
    {
        if (!DisponibleEnNatural)
            return false;
        DisponibleEnNatural = false;
        return true;
    }

    // Lo único que el personal ajusta en Citas: duración y disponibilidad para agendar.
    public void AjustarEnCitas(int tiempoEstimado, bool activo)
    {
        ValidarTiempo(tiempoEstimado);
        TiempoEstimado = tiempoEstimado;
        Activo = activo;
    }

    // La persistencia asigna el id real una sola vez, tras el primer guardado.
    public void EstablecerIdentidad(int id)
    {
        if (IdServicio == 0) IdServicio = id;
    }

    private static void ValidarTiempo(int tiempoEstimado)
    {
        if (tiempoEstimado <= 0)
            throw new DomainException("El tiempo estimado debe ser mayor que cero (minutos).");
    }
}
