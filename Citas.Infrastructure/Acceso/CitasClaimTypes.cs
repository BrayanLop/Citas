using Citas.Multitenancy;

namespace Citas.Infrastructure.Acceso;

/// <summary>
/// Claims "normalizados" con los que trabaja Citas, sea cual sea el token de origen.
/// El token de Natural se traduce a estos claims al validarse (ver Program.cs) y el
/// token de cliente de Citas ya se emite con ellos.
/// </summary>
public static class CitasClaimTypes
{
    public const string Tenant = TenantClaimTypes.Tenant;
    public const string Modo = "mode";
    public const string Nombre = "name";

    /// <summary>Id del Usuario de Citas en el tenant.</summary>
    public const string UsuarioId = "userId";

    /// <summary>Persona global del cliente (tabla Personas de Citas).</summary>
    public const string PersonaId = "personaId";

    /// <summary>Id de la persona en Natural (claim 'sub' del token de Natural).</summary>
    public const string PersonaNaturalId = "personaNaturalId";

    /// <summary>Código de rol de Natural ('01', '02', '03').</summary>
    public const string RolNatural = "rolNatural";

    public const string ModoPersonal = "personal";
    public const string ModoCliente = "cliente";
}

/// <summary>Claims tal como los emite Natural en su JWT.</summary>
public static class NaturalClaimTypes
{
    public const string EmpresaId = "empresaId";

    // Natural arma el token con JwtSecurityToken directamente, así que el nombre y el rol
    // pueden llegar con el tipo largo de .NET o con el corto; se aceptan ambos.
    public static readonly string[] PersonaId =
        ["sub", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"];

    public static readonly string[] Nombre =
        ["name", "unique_name", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"];

    public static readonly string[] Rol =
        ["role", "roles", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
}
