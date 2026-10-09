namespace Citas.Application.Sincronizacion;

/// <summary>Sincroniza el catálogo (servicios y personal) del tenant actual con Natural.</summary>
public interface ISincronizacionAppService
{
    /// <summary>
    /// Lee servicios y personas de Natural con el token de la petición y hace upsert en el
    /// tenant actual, todo en una transacción. Nada se borra: lo que ya no está en Natural
    /// queda no agendable (servicios) o inactivo (personal).
    /// </summary>
    Task<ResumenSincronizacionDto> SincronizarConNaturalAsync(CancellationToken cancellationToken = default);
}
