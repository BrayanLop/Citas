using Citas.Application.Common;
using Citas.Domain.Acceso;
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
            return existente.Id;

        // Sin agenda activada no se crea nada: primero un administrador activa la empresa.
        var empresa = await _empresas.ObtenerPorIdAsync(tenant, cancellationToken);
        if (empresa is null || !empresa.Activo)
            return null;

        var nombreUsuario = string.IsNullOrWhiteSpace(nombre) ? $"Natural {personaNaturalId}" : nombre;
        var usuario = Usuario.CrearParaPersonalNatural(nombreUsuario, personaNaturalId);
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

        await AsignadorRoles.AsignarAsync(_roles, _uow, usuario.Id, tenant, AsignadorRoles.Empleado, cancellationToken);
        return usuario.Id;
    }
}
