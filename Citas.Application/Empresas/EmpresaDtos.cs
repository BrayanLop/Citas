namespace Citas.Application.Empresas;

public class CreateEmpresaDto
{
    /// <summary>Código único de la empresa (se usa como TenantId), ej: "ACME".</summary>
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}

public class EmpresaResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
