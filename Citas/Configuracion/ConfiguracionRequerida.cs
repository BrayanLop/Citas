namespace Citas.Configuracion;

/// <summary>
/// Comprueba al arrancar que exista la configuración sin la cual la API no puede
/// funcionar, y falla con un mensaje que dice exactamente qué variable falta.
/// Los valores llegan por variables de entorno (ConnectionStrings__DefaultConnection,
/// Jwt__Key, NaturalJwt__Key, NaturalApi__BaseUrl...), user-secrets o un appsettings.json local no versionado.
/// </summary>
public static class ConfiguracionRequerida
{
    // HMAC-SHA256 exige claves de al menos 256 bits.
    private const int LongitudMinimaClave = 32;

    public const string IssuerNaturalPorDefecto = "Natural.Api";
    public const string AudienceNaturalPorDefecto = "Natural.Client";

    public static void Validar(IConfiguration config)
    {
        var faltantes = new List<string>();

        Requerir(config, "ConnectionStrings:DefaultConnection", "ConnectionStrings__DefaultConnection", faltantes);
        Requerir(config, "Jwt:Issuer", "Jwt__Issuer", faltantes);
        Requerir(config, "Jwt:Audience", "Jwt__Audience", faltantes);
        Requerir(config, "Jwt:Key", "Jwt__Key", faltantes);
        Requerir(config, "NaturalJwt:Key", "NaturalJwt__Key", faltantes);
        Requerir(config, "NaturalApi:BaseUrl", "NaturalApi__BaseUrl", faltantes);

        if (faltantes.Count > 0)
            throw new InvalidOperationException(
                "Falta configuración obligatoria de Citas: " + string.Join(", ", faltantes) +
                ". Defínela como variable de entorno (ver .env.example y el README).");

        ValidarLongitud(config["Jwt:Key"]!, "Jwt__Key");
        ValidarLongitud(config["NaturalJwt:Key"]!, "NaturalJwt__Key");

        if (!Uri.TryCreate(config["NaturalApi:BaseUrl"], UriKind.Absolute, out var naturalApi) ||
            (naturalApi.Scheme != Uri.UriSchemeHttp && naturalApi.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException(
                "NaturalApi__BaseUrl debe ser una URL absoluta http(s), p. ej. https://naturalback.vip/v2/api.");

        // El esquema se elige por el 'iss' del token: si coincidieran no se podrían distinguir.
        if (string.Equals(config["Jwt:Issuer"], NaturalIssuer(config), StringComparison.Ordinal))
            throw new InvalidOperationException(
                "Jwt__Issuer (tokens de cliente de Citas) no puede ser igual al issuer de Natural.");
    }

    public static string NaturalIssuer(IConfiguration config)
        => Valor(config, "NaturalJwt:Issuer") ?? IssuerNaturalPorDefecto;

    public static string NaturalAudience(IConfiguration config)
        => Valor(config, "NaturalJwt:Audience") ?? AudienceNaturalPorDefecto;

    private static void Requerir(IConfiguration config, string clave, string variable, List<string> faltantes)
    {
        if (Valor(config, clave) is null)
            faltantes.Add(variable);
    }

    private static void ValidarLongitud(string clave, string variable)
    {
        if (clave.Length < LongitudMinimaClave)
            throw new InvalidOperationException(
                $"{variable} debe tener al menos {LongitudMinimaClave} caracteres (HMAC-SHA256).");
    }

    private static string? Valor(IConfiguration config, string clave)
    {
        var valor = config[clave];
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }
}
