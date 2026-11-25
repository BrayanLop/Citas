using System.Security.Cryptography;
using System.Text;

namespace Citas.Application.Calendario;

/// <summary>
/// Firmas HMAC de la integración con Google:
///  - el "state" de OAuth (a qué empresa y empleado pertenece el permiso que Google devuelve,
///    con vencimiento, para que nadie pueda conectar una cuenta a otro empleado);
///  - el token del canal de notificaciones (para aceptar solo avisos que pedimos nosotros).
/// La clave sale de la misma clave de cifrado; cada uso lleva su propio contexto.
/// </summary>
public sealed class FirmasGoogle
{
    public static readonly TimeSpan VigenciaEstado = TimeSpan.FromMinutes(15);

    private readonly byte[] _claveEstado;
    private readonly byte[] _claveCanal;

    public FirmasGoogle(OpcionesGoogle opciones)
    {
        var clave = string.IsNullOrWhiteSpace(opciones.ClaveCifradoBase64)
            ? new byte[32]   // integración apagada: nunca se firma nada real
            : Convert.FromBase64String(opciones.ClaveCifradoBase64);
        _claveEstado = HMACSHA256.HashData(clave, "citas-google-oauth-state"u8);
        _claveCanal = HMACSHA256.HashData(clave, "citas-google-channel-token"u8);
    }

    public string CrearEstado(string tenant, int idEmpleado, DateTime ahoraUtc)
    {
        var vence = new DateTimeOffset(DateTime.SpecifyKind(ahoraUtc, DateTimeKind.Utc)).Add(VigenciaEstado).ToUnixTimeSeconds();
        var carga = Base64Url(Encoding.UTF8.GetBytes($"{tenant}|{idEmpleado}|{vence}"));
        return $"{carga}.{Firmar(_claveEstado, carga)}";
    }

    public (string Tenant, int IdEmpleado)? ValidarEstado(string? estado, DateTime ahoraUtc)
    {
        if (string.IsNullOrWhiteSpace(estado)) return null;
        var partes = estado.Split('.');
        if (partes.Length != 2 || !IgualesEnTiempoConstante(Firmar(_claveEstado, partes[0]), partes[1])) return null;

        string texto;
        try { texto = Encoding.UTF8.GetString(DeBase64Url(partes[0])); }
        catch (FormatException) { return null; }

        var campos = texto.Split('|');
        if (campos.Length != 3 || !int.TryParse(campos[1], out var empleado) || !long.TryParse(campos[2], out var vence))
            return null;
        if (new DateTimeOffset(DateTime.SpecifyKind(ahoraUtc, DateTimeKind.Utc)).ToUnixTimeSeconds() > vence) return null;
        return string.IsNullOrEmpty(campos[0]) || empleado <= 0 ? null : (campos[0], empleado);
    }

    public string TokenCanal(string canalId) => Firmar(_claveCanal, canalId);

    public bool TokenCanalValido(string canalId, string? token)
        => !string.IsNullOrEmpty(token) && IgualesEnTiempoConstante(TokenCanal(canalId), token);

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] DeBase64Url(string texto)
    {
        var s = texto.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
    }

    private static string Firmar(byte[] clave, string texto)
        => Base64Url(HMACSHA256.HashData(clave, Encoding.UTF8.GetBytes(texto)));

    private static bool IgualesEnTiempoConstante(string a, string b)
        => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
