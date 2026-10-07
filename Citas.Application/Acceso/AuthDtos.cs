namespace Citas.Application.Acceso;

/// <summary>Registro de un cliente externo (identidad global en Persona).</summary>
public class RegisterClienteDto
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Celular { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class LoginClienteDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>Respuesta del login/registro global del cliente (token sin tenant).</summary>
public class ClienteAuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public int PersonaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime ExpiraUtc { get; set; }
}

public class SelectEmpresaDto
{
    public string Tenant { get; set; } = string.Empty;
}

/// <summary>Token con tenant tras elegir empresa; incluye el userId del cliente en ese tenant.</summary>
public class SelectEmpresaResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Tenant { get; set; } = string.Empty;
    public string EmpresaNombre { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime ExpiraUtc { get; set; }
}
