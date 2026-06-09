namespace Citas.Application.Acceso;

public interface IRolAppService
{
    Task<IReadOnlyList<RolResponseDto>> ListarAsync(CancellationToken cancellationToken = default);
}
