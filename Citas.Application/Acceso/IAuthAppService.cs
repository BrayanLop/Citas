namespace Citas.Application.Acceso;

/// <summary>Casos de uso de autenticación y emisión de tokens.</summary>
public interface IAuthAppService
{
    /// <summary>Emite un token con el claim 'tenant' para la empresa indicada (puente de empleado).</summary>
    Task<TokenResponseDto> GenerarTokenAsync(TokenRequestDto dto, CancellationToken cancellationToken = default);

    /// <summary>Registra un cliente externo (Persona global con contraseña).</summary>
    Task<ClienteAuthResponseDto> RegisterAsync(RegisterClienteDto dto, CancellationToken cancellationToken = default);

    /// <summary>Login global del cliente (email + contraseña).</summary>
    Task<ClienteAuthResponseDto> LoginAsync(LoginClienteDto dto, CancellationToken cancellationToken = default);

    /// <summary>Tras elegir empresa, asegura el usuario en el tenant y emite un token con 'tenant'.</summary>
    Task<SelectEmpresaResponseDto> SelectEmpresaAsync(int personaId, SelectEmpresaDto dto, CancellationToken cancellationToken = default);

    /// <summary>Puente Natural → Citas: asegura el empleado en el tenant y emite token modo empresa.</summary>
    Task<SelectEmpresaResponseDto> TokenEmpleadoAsync(EmpleadoTokenRequestDto dto, CancellationToken cancellationToken = default);
}
