namespace Citas.Application.Empresas;

/// <summary>
/// Datos para activar la agenda de la empresa del usuario de Natural. El código de la
/// empresa (tenant) NO viaja en el body: se toma siempre del empresaId del token.
/// </summary>
public class ActivarAgendaDto
{
    /// <summary>Nombre visible de la empresa. Obligatorio la primera vez.</summary>
    public string? Nombre { get; set; }
}

/// <summary>Empresa tal como la ve un cliente al elegir dónde agendar.</summary>
public class EmpresaPublicaDto
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}

public class EmpresaResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
