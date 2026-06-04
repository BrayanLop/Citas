namespace Citas.Multitenancy;

/// <summary>Nombres de claims usados para la resolucion del tenant.</summary>
public static class TenantClaimTypes
{
    /// <summary>Claim que contiene el codigo del tenant/empresa dentro del JWT.</summary>
    public const string Tenant = "tenant";
}
