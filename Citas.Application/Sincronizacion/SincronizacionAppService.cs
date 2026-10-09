using Citas.Application.Acceso;
using Citas.Application.Acceso.Abstractions;
using Citas.Application.Common;
using Citas.Application.Integracion;
using Citas.Domain.Common;
using Citas.Domain.Servicios;
using Citas.Models;

namespace Citas.Application.Sincronizacion;

// Reglas de la sincronización con Natural (Natural es la fuente del catálogo):
//  - Servicios: se vinculan por (tenant, ServicioNaturalId). Los nuevos nacen con la
//    duración por defecto y agendables según 'disponible' de Natural; en los existentes solo
//    se copian nombre, valor y disponibilidad de Natural (TiempoEstimado y Activo son de
//    Citas). Los que ya no vienen de Natural quedan no agendables; nunca se borran.
//  - Personal: lo resuelve PersonalAppService (alta/actualización por PersonaNaturalId e
//    inactivación de quien ya no está en Natural).
public class SincronizacionAppService : ISincronizacionAppService
{
    private readonly INaturalCatalogo _natural;
    private readonly IServicioRepository _servicios;
    private readonly IPersonalAppService _personal;
    private readonly IUsuarioActual _actual;
    private readonly IUnitOfWork _uow;
    private readonly OpcionesSincronizacion _opciones;

    public SincronizacionAppService(
        INaturalCatalogo natural,
        IServicioRepository servicios,
        IPersonalAppService personal,
        IUsuarioActual actual,
        IUnitOfWork uow,
        OpcionesSincronizacion opciones)
    {
        _natural = natural;
        _servicios = servicios;
        _personal = personal;
        _actual = actual;
        _uow = uow;
        _opciones = opciones;
    }

    public async Task<ResumenSincronizacionDto> SincronizarConNaturalAsync(CancellationToken cancellationToken = default)
    {
        var tenant = _actual.Tenant
            ?? throw new ForbiddenAppException("La petición no trae empresa (tenant).");

        // Primero se lee todo de Natural: si falla, no se toca nada en Citas.
        var servicios = DeLaEmpresa(await _natural.ObtenerServiciosAsync(cancellationToken), s => s.Id, s => s.EmpresaId, tenant);
        var personas = DeLaEmpresa(await _natural.ObtenerPersonasAsync(cancellationToken), p => p.Id, p => p.EmpresaId, tenant);

        return await _uow.EjecutarEnTransaccionAsync(async ct => new ResumenSincronizacionDto
        {
            Servicios = await SincronizarServiciosAsync(servicios, ct),
            Personal = await _personal.SincronizarAsync(tenant, personas, _actual.UsuarioId, ct),
        }, cancellationToken);
    }

    private async Task<ResumenCambiosDto> SincronizarServiciosAsync(
        IReadOnlyList<ServicioNatural> deNatural,
        CancellationToken cancellationToken)
    {
        var resumen = new ResumenCambiosDto();
        var existentes = (await _servicios.ListarAsync(cancellationToken))
            .ToDictionary(s => s.ServicioNaturalId);

        foreach (var item in deNatural)
        {
            if (existentes.Remove(item.Id, out var servicio))
            {
                var estabaDisponible = servicio.DisponibleEnNatural;
                if (!servicio.SincronizarDesdeNatural(item.Nombre, item.Precio, item.Disponible))
                    continue;

                _servicios.Actualizar(servicio);
                if (estabaDisponible && !item.Disponible)
                    resumen.Desactivados++;
                else
                    resumen.Actualizados++;
            }
            else
            {
                var nuevo = Servicio.CrearDesdeNatural(
                    item.Id, item.Nombre, item.Precio, item.Disponible, _opciones.TiempoEstimadoPorDefecto);
                await _servicios.AgregarAsync(nuevo, cancellationToken);
                resumen.Creados++;
            }
        }

        // Lo que queda en el diccionario ya no viene de Natural.
        foreach (var retirado in existentes.Values)
        {
            if (!retirado.RetirarDeNatural())
                continue;
            _servicios.Actualizar(retirado);
            resumen.Desactivados++;
        }

        await _uow.GuardarCambiosAsync(cancellationToken);
        return resumen;
    }

    // Natural ya filtra por la empresa del token; por si acaso se descarta lo que traiga
    // otra empresa, ids no válidos y duplicados.
    private static IReadOnlyList<T> DeLaEmpresa<T>(
        IEnumerable<T> items,
        Func<T, int> id,
        Func<T, int?> empresaId,
        string tenant)
        => items
            .Where(i => id(i) > 0)
            .Where(i => empresaId(i) is not { } empresa || empresa.ToString() == tenant)
            .GroupBy(id)
            .Select(g => g.First())
            .ToList();
}
