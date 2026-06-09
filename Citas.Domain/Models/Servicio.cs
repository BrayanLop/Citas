using Citas.Domain.Common;

namespace Citas.Models;

/// <summary>
/// Raíz de agregado del catálogo de servicios de un tenant. Se crea con
/// <see cref="Crear"/> y se modifica con <see cref="Actualizar"/>.
/// </summary>
public class Servicio : ITenantEntity
{
    // Constructor sin parámetros para EF Core.
    private Servicio() { }

    public int IdServicio { get; private set; }
    public string NombreServicio { get; private set; } = string.Empty;
    public decimal Valor { get; private set; }
    public int TiempoEstimado { get; private set; }
    public string TenantId { get; set; } = string.Empty;

    public ICollection<DetalleCita> DetalleCitas { get; private set; } = [];

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

    public void Actualizar(string nombreServicio, decimal valor, int tiempoEstimado)
    {
        Validar(nombreServicio, valor, tiempoEstimado);
        NombreServicio = nombreServicio.Trim();
        Valor = valor;
        TiempoEstimado = tiempoEstimado;
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
