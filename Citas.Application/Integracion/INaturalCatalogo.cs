namespace Citas.Application.Integracion;

/// <summary>Servicio tal como lo publica Natural (GET Servicio/Obtener).</summary>
public sealed record ServicioNatural(int Id, string? Nombre, decimal Precio, bool Disponible, int? EmpresaId);

/// <summary>Persona (personal de la empresa) tal como la publica Natural (GET Persona/Obtener).</summary>
public sealed record PersonaNatural(int Id, string? Nombre, string? Apellido, string? Email, string? Celular, int? EmpresaId)
{
    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
}

/// <summary>
/// Puerto de lectura del catálogo de Natural (servicios y personal de la empresa). La
/// implementación llama a la API de Natural reenviando el token de Natural de la petición
/// actual; Natural toma la empresa de ese token. Los fallos llegan como
/// <see cref="IntegracionNaturalException"/> (403 si Natural rechaza el token, 502 si no responde).
/// </summary>
public interface INaturalCatalogo
{
    Task<IReadOnlyList<ServicioNatural>> ObtenerServiciosAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonaNatural>> ObtenerPersonasAsync(CancellationToken cancellationToken = default);
}
