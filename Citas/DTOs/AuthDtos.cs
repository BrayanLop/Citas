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
