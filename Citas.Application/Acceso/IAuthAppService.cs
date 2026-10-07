namespace Citas.Application.Acceso;

/// <summary>
/// Casos de uso de autenticación de clientes externos. El personal de la empresa no pasa
/// por aquí: entra a Citas con su token de Natural.
/// </summary>
public interface IAuthAppService
{
    /// <summary>Registra un cliente externo (Persona global con contraseña).</summary>
    Task<ClienteAuthResponseDto> RegisterAsync(RegisterClienteDto dto, CancellationToken cancellationToken = default);

    /// <summary>Login global del cliente (email + contraseña).</summary>
    Task<ClienteAuthResponseDto> LoginAsync(LoginClienteDto dto, CancellationToken cancellationToken = default);

    /// <summary>Tras elegir empresa, asegura el usuario en el tenant y emite un token con 'tenant'.</summary>
    Task<SelectEmpresaResponseDto> SelectEmpresaAsync(int personaId, SelectEmpresaDto dto, CancellationToken cancellationToken = default);
}
