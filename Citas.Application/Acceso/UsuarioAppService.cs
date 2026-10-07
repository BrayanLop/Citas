using Citas.Application.Acceso.Abstractions;
using Citas.Application.Common;
using Citas.Domain.Acceso;
using Citas.Models;

namespace Citas.Application.Acceso;

public class UsuarioAppService : IUsuarioAppService
{
    private readonly IUsuarioRepository _usuarios;
    private readonly IUsuarioActual _actual;
    private readonly IUnitOfWork _uow;

    public UsuarioAppService(IUsuarioRepository usuarios, IUsuarioActual actual, IUnitOfWork uow)
    {
        _usuarios = usuarios;
        _actual = actual;
        _uow = uow;
    }

    public async Task<IReadOnlyList<UsuarioResponseDto>> ListarAsync(string? rol = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<Usuario> usuarios = await _usuarios.ListarConRolesAsync(cancellationToken);

        // El cliente solo ve a los empleados activos (para elegir con quién agendar).
        if (_actual.EsCliente)
            usuarios = usuarios.Where(u => u.EsPersonalNatural && u.EstaActivo);

        var response = usuarios.Select(MapToDto);

        if (!string.IsNullOrWhiteSpace(rol))
            response = response.Where(u => string.Equals(u.Rol, rol, StringComparison.OrdinalIgnoreCase));

        return response.ToList();
    }

    public async Task<UsuarioResponseDto> CrearAsync(CreateUsuarioDto dto, CancellationToken cancellationToken = default)
    {
        var usuario = Usuario.Crear(dto.NombreUsuario);
        // tenant = null: el contexto lo toma del tenant actual del token.
        await _usuarios.AgregarAsync(usuario, null, cancellationToken);
        await _uow.GuardarCambiosAsync(cancellationToken);
        return MapToDto(usuario);
    }

    public async Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarios.ObtenerPorIdAsync(id, cancellationToken);
        if (usuario is null)
            return false;

        _usuarios.Eliminar(usuario);
        await _uow.GuardarCambiosAsync(cancellationToken);
        return true;
    }

    private static UsuarioResponseDto MapToDto(Usuario u) => new()
    {
        Id = u.Id,
        NombreUsuario = u.NombreUsuario,
        PersonaId = u.PersonaId,
        PersonaNaturalId = u.PersonaNaturalId,
        Rol = u.RolUsuarios.Select(ru => ru.Rol?.NombreRol).FirstOrDefault(),
    };
}
