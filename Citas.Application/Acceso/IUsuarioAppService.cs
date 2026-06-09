namespace Citas.Application.Acceso;

public interface IUsuarioAppService
{
    /// <summary>Lista los usuarios del tenant; filtra opcionalmente por nombre de rol.</summary>
    Task<IReadOnlyList<UsuarioResponseDto>> ListarAsync(string? rol = null, CancellationToken cancellationToken = default);

    Task<UsuarioResponseDto> CrearAsync(CreateUsuarioDto dto, CancellationToken cancellationToken = default);

    /// <summary>Devuelve false si el usuario no existe.</summary>
    Task<bool> EliminarAsync(int id, CancellationToken cancellationToken = default);
}
