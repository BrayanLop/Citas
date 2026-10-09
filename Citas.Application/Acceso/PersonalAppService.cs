using Citas.Application.Common;
using Citas.Application.Integracion;
using Citas.Application.Sincronizacion;
using Citas.Domain.Acceso;
using Citas.Domain.Common;
using Citas.Domain.Empresas;
using Citas.Models;

namespace Citas.Application.Acceso;

public class PersonalAppService : IPersonalAppService
{
    private readonly IEmpresaRepository _empresas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IRolRepository _roles;
    private readonly IUnitOfWork _uow;

    public PersonalAppService(
        IEmpresaRepository empresas,
        IUsuarioRepository usuarios,
        IRolRepository roles,
        IUnitOfWork uow)
    {
        _empresas = empresas;
        _usuarios = usuarios;
        _roles = roles;
        _uow = uow;
    }

    public async Task<int?> AsegurarUsuarioAsync(
        string tenant,
        int personaNaturalId,
        string? nombre,
        CancellationToken cancellationToken = default)
    {
        var existente = await _usuarios.BuscarEnTenantPorPersonaNaturalAsync(tenant, personaNaturalId, cancellationToken);
        if (existente is not null)
        {
            if (!existente.EstaActivo)
                throw new ForbiddenAppException(
                    "Tu usuario está inactivo en la agenda de esta empresa (ya no figura en Natural). " +
                    "Pide a un administrador que sincronice con Natural.");
            return existente.Id;
        }

        // Sin agenda activada no se crea nada: primero un administrador activa la empresa.
        var empresa = await _empresas.ObtenerPorIdAsync(tenant, cancellationToken);
        if (empresa is null || !empresa.Activo)
            return null;

        var usuario = NuevoPersonal(personaNaturalId, nombre);
        await _usuarios.AgregarAsync(usuario, tenant, cancellationToken);

        try
        {
            await _uow.GuardarCambiosAsync(cancellationToken);
        }
        catch
        {
            // Dos peticiones simultáneas de la misma persona: la otra ganó la carrera y el
            // índice único (tenant, persona de Natural) rechazó esta alta. Usamos la suya.
            _uow.DescartarCambios();
            var creadoPorOtra = await _usuarios.BuscarEnTenantPorPersonaNaturalAsync(tenant, personaNaturalId, cancellationToken);
            if (creadoPorOtra is null)
                throw;
            return creadoPorOtra.Id;
        }

        await AsignarRolEmpleadoAsync(usuario, tenant, cancellationToken);
        return usuario.Id;
    }

    public async Task<ResumenCambiosDto> SincronizarAsync(
        string tenant,
        IReadOnlyList<PersonaNatural> personas,
        int? usuarioActualId,
        CancellationToken cancellationToken = default)
    {
        var resumen = new ResumenCambiosDto();
        var existentes = (await _usuarios.ListarPersonalNaturalAsync(cancellationToken))
            .ToDictionary(u => u.PersonaNaturalId!.Value);
        var nuevos = new List<Usuario>();

        foreach (var persona in personas)
        {
            if (existentes.Remove(persona.Id, out var usuario))
            {
                if (!usuario.SincronizarDesdeNatural(persona.NombreCompleto, persona.Email, persona.Celular))
                    continue;
                _usuarios.Actualizar(usuario);
                resumen.Actualizados++;
            }
            else
            {
                var nuevo = NuevoPersonal(persona.Id, persona.NombreCompleto);
                nuevo.SincronizarDesdeNatural(persona.NombreCompleto, persona.Email, persona.Celular);
                await _usuarios.AgregarAsync(nuevo, tenant, cancellationToken);
                nuevos.Add(nuevo);
            }
        }

        // Lo que queda ya no está en Natural: inactivo, no se borra (puede tener citas).
        // Nunca se desactiva a quien está sincronizando, para no dejarlo fuera.
        foreach (var retirado in existentes.Values)
        {
            if (retirado.Id == usuarioActualId || !retirado.Desactivar())
                continue;
            _usuarios.Actualizar(retirado);
            resumen.Desactivados++;
        }

        await _uow.GuardarCambiosAsync(cancellationToken);

        foreach (var nuevo in nuevos)
            await AsignarRolEmpleadoAsync(nuevo, tenant, cancellationToken);
        resumen.Creados = nuevos.Count;

        return resumen;
    }

    // Alta del personal de Natural: la comparten el alta automática y la sincronización.
    private static Usuario NuevoPersonal(int personaNaturalId, string? nombre)
        => Usuario.CrearParaPersonalNatural(
            string.IsNullOrWhiteSpace(nombre) ? $"Natural {personaNaturalId}" : nombre,
            personaNaturalId);

    private Task AsignarRolEmpleadoAsync(Usuario usuario, string tenant, CancellationToken cancellationToken)
        => AsignadorRoles.AsignarAsync(_roles, _uow, usuario.Id, tenant, AsignadorRoles.Empleado, cancellationToken);
}
