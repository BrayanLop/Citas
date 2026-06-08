namespace Citas.DTOs;

public class TokenRequestDto
{
    /// <summary>Codigo de la empresa/tenant para la que se emite el token.</summary>
    public string Tenant { get; set; } = string.Empty;
    /// <summary>Nombre de usuario (informativo para el token).</summary>
    public string Usuario { get; set; } = string.Empty;
}

public class TokenResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Tenant { get; set; } = string.Empty;
    public DateTime ExpiraUtc { get; set; }
}

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

/// <summary>Puente para que un empleado de Natural entre a Citas en una empresa/tenant.</summary>
public class EmpleadoTokenRequestDto
{
    public string Tenant { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    /// <summary>JWT de Natural (se valida si hay clave compartida configurada).</summary>
    public string? NaturalToken { get; set; }
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
