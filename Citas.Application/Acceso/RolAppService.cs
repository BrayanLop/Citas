using Citas.Domain.Acceso;
using Citas.Models;

namespace Citas.Application.Acceso;

public class RolAppService : IRolAppService
{
    private readonly IRolRepository _roles;

    public RolAppService(IRolRepository roles) => _roles = roles;

    public async Task<IReadOnlyList<RolResponseDto>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roles.ListarAsync(cancellationToken);
        return roles.Select(MapToDto).ToList();
    }

    private static RolResponseDto MapToDto(Rol r) => new()
    {
        Id = r.Id,
        NombreRol = r.NombreRol,
    };
}
