namespace Citas.Application.Acceso.Abstractions;

/// <summary>Cómo entró el usuario de la petición actual.</summary>
public enum ModoAcceso
{
    /// <summary>Sin token (o token no válido).</summary>
    Anonimo,
    /// <summary>Personal de la empresa, autenticado con el token de Natural.</summary>
    Personal,
    /// <summary>Cliente externo, autenticado con un token emitido por Citas.</summary>
    Cliente,
}

/// <summary>Códigos de rol que emite Natural en su JWT.</summary>
public static class RolesNatural
{
    public const string Administrador = "01";
    public const string Trabajador = "02";
    public const string SuperEmpleado = "03";

    /// <summary>Roles que pueden administrar la agenda (servicios, usuarios, empresa).</summary>
    public static bool EsAdministrativo(string? rol) => rol is Administrador or SuperEmpleado;
}

/// <summary>
/// Usuario de la petición actual, tal como lo dejó la autenticación (token de Natural
/// para el personal o token de Citas para el cliente). Es el punto único que consultan
/// los casos de uso para aplicar reglas por modo/rol; la implementación lee los claims.
/// </summary>
public interface IUsuarioActual
{
    ModoAcceso Modo { get; }

    /// <summary>Tenant (empresa) de la petición: empresaId de Natural o el elegido por el cliente.</summary>
    string? Tenant { get; }

    /// <summary>Id del Usuario de Citas dentro del tenant (personal ya provisionado o cliente).</summary>
    int? UsuarioId { get; }

    /// <summary>Persona global del cliente (solo modo cliente).</summary>
    int? PersonaId { get; }

    /// <summary>Id de la persona en Natural (solo modo personal).</summary>
    int? PersonaNaturalId { get; }

    /// <summary>Código de rol de Natural ('01', '02', '03'); solo modo personal.</summary>
    string? RolNatural { get; }

    string? Nombre { get; }

    bool EsPersonal => Modo == ModoAcceso.Personal;
    bool EsCliente => Modo == ModoAcceso.Cliente;

    /// <summary>Personal con rol administrativo de Natural ('01' o '03').</summary>
    bool EsAdministrador => EsPersonal && RolesNatural.EsAdministrativo(RolNatural);
}
