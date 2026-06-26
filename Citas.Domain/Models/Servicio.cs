using Citas.Domain.Common;

namespace Citas.Models;

// Servicio del catálogo de una empresa. Se crea con Crear() y se edita con Actualizar();
// ambos pasan por la misma validación.
public class Servicio
{
    private Servicio() { }

    public int IdServicio { get; private set; }
    public string NombreServicio { get; private set; } = string.Empty;
    public decimal Valor { get; private set; }
    public int TiempoEstimado { get; private set; }

    public static Servicio Crear(string nombreServicio, decimal valor, int tiempoEstimado)
    {
        Validar(nombreServicio, valor, tiempoEstimado);
        return new Servicio
        {
            NombreServicio = nombreServicio.Trim(),
            Valor = valor,
            TiempoEstimado = tiempoEstimado,
        };
    }

    public static Servicio Rehidratar(int idServicio, string nombreServicio, decimal valor, int tiempoEstimado)
        => new()
        {
            IdServicio = idServicio,
            NombreServicio = nombreServicio,
            Valor = valor,
            TiempoEstimado = tiempoEstimado,
        };

    public void Actualizar(string nombreServicio, decimal valor, int tiempoEstimado)
    {
        Validar(nombreServicio, valor, tiempoEstimado);
        NombreServicio = nombreServicio.Trim();
        Valor = valor;
        TiempoEstimado = tiempoEstimado;
    }

    // La persistencia asigna el id real una sola vez, tras el primer guardado.
    public void EstablecerIdentidad(int id)
    {
        if (IdServicio == 0) IdServicio = id;
    }

    private static void Validar(string nombreServicio, decimal valor, int tiempoEstimado)
    {
        if (string.IsNullOrWhiteSpace(nombreServicio))
            throw new DomainException("El nombre del servicio es obligatorio.");
        if (valor < 0)
            throw new DomainException("El valor del servicio no puede ser negativo.");
        if (tiempoEstimado < 0)
            throw new DomainException("El tiempo estimado no puede ser negativo.");
    }
}
