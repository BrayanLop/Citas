using Citas.Domain.Common;

namespace Citas.Domain.Calendario;

/// <summary>
/// Conexión de un empleado con su Google Calendar. Guarda el refresh token CIFRADO (nunca en
/// claro) y el estado de la sincronización. Si Google revoca el acceso, pasa a Revocada: la
/// agenda de Citas sigue funcionando y el empleado ve "reconectar".
/// </summary>
public class CuentaGoogle
{
    public const string CalendarioPrincipal = "primary";
    public const int MaximoMinutosReintento = 60;

    private CuentaGoogle() { }

    public int IdCuenta { get; private set; }
    public int IdEmpleado { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string RefreshTokenCifrado { get; private set; } = string.Empty;
    public EstadoCuentaGoogle Estado { get; private set; } = EstadoCuentaGoogle.Activa;
    public string CalendarioId { get; private set; } = CalendarioPrincipal;

    /// <summary>Token de sincronización incremental de Google (null = falta una carga completa).</summary>
    public string? SyncToken { get; private set; }
    public DateTime? UltimaSincronizacionUtc { get; private set; }

    /// <summary>Un aviso (webhook) de Google dijo que hay cambios: sincronizar sin esperar el intervalo.</summary>
    public bool CambioRemotoPendiente { get; private set; }

    public int Intentos { get; private set; }
    public DateTime? ProximoIntentoUtc { get; private set; }
    public string? UltimoError { get; private set; }

    // Canal de notificaciones push de Google (events.watch).
    public string? CanalId { get; private set; }
    public DateTime? CanalExpiraUtc { get; private set; }

    public DateTime ConectadaUtc { get; private set; }

    public bool EstaActiva => Estado == EstadoCuentaGoogle.Activa;

    public static CuentaGoogle Conectar(int idEmpleado, string email, string refreshTokenCifrado, DateTime ahoraUtc)
    {
        if (idEmpleado <= 0) throw new DomainException("La conexión requiere un empleado válido.");
        if (string.IsNullOrWhiteSpace(refreshTokenCifrado)) throw new DomainException("Falta el token de acceso a Google.");
        return new CuentaGoogle
        {
            IdEmpleado = idEmpleado,
            Email = email?.Trim() ?? string.Empty,
            RefreshTokenCifrado = refreshTokenCifrado,
            ConectadaUtc = ahoraUtc,
        };
    }

    public static CuentaGoogle Rehidratar(
        int idCuenta, int idEmpleado, string email, string refreshTokenCifrado, EstadoCuentaGoogle estado,
        string calendarioId, string? syncToken, DateTime? ultimaSincronizacionUtc, bool cambioRemotoPendiente,
        int intentos, DateTime? proximoIntentoUtc, string? ultimoError, string? canalId, DateTime? canalExpiraUtc,
        DateTime conectadaUtc)
        => new()
        {
            IdCuenta = idCuenta, IdEmpleado = idEmpleado, Email = email, RefreshTokenCifrado = refreshTokenCifrado,
            Estado = estado, CalendarioId = calendarioId, SyncToken = syncToken,
            UltimaSincronizacionUtc = Utc(ultimaSincronizacionUtc), CambioRemotoPendiente = cambioRemotoPendiente,
            Intentos = intentos, ProximoIntentoUtc = Utc(proximoIntentoUtc), UltimoError = ultimoError,
            CanalId = canalId, CanalExpiraUtc = Utc(canalExpiraUtc), ConectadaUtc = DateTime.SpecifyKind(conectadaUtc, DateTimeKind.Utc),
        };

    /// <summary>El empleado vuelve a conectar (tras una revocación o con otra cuenta de Google).</summary>
    public void Reconectar(string email, string refreshTokenCifrado, DateTime ahoraUtc)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenCifrado)) throw new DomainException("Falta el token de acceso a Google.");
        Email = email?.Trim() ?? string.Empty;
        RefreshTokenCifrado = refreshTokenCifrado;
        Estado = EstadoCuentaGoogle.Activa;
        SyncToken = null;
        CambioRemotoPendiente = false;
        Intentos = 0;
        ProximoIntentoUtc = null;
        UltimoError = null;
        CanalId = null;
        CanalExpiraUtc = null;
        ConectadaUtc = ahoraUtc;
    }

    /// <summary>Google ya no acepta el token (el empleado retiró el permiso o venció): hay que reconectar.</summary>
    public void Revocar(string? motivo)
    {
        Estado = EstadoCuentaGoogle.Revocada;
        SyncToken = null;
        CambioRemotoPendiente = false;
        CanalId = null;
        CanalExpiraUtc = null;
        UltimoError = Recortar(motivo);
    }

    public void RegistrarSincronizacionEntrante(string? syncToken, DateTime ahoraUtc)
    {
        SyncToken = syncToken;
        UltimaSincronizacionUtc = ahoraUtc;
        CambioRemotoPendiente = false;
    }

    public void InvalidarSyncToken() => SyncToken = null;

    public void MarcarCambioRemoto() => CambioRemotoPendiente = true;

    public void RegistrarExito()
    {
        Intentos = 0;
        ProximoIntentoUtc = null;
        UltimoError = null;
    }

    /// <summary>Un fallo transitorio: reintento con espera creciente (1, 2, 4... hasta 60 min).</summary>
    public void RegistrarFallo(string error, DateTime ahoraUtc)
    {
        Intentos++;
        var minutos = Math.Min(MaximoMinutosReintento, 1 << Math.Min(Intentos - 1, 6));
        ProximoIntentoUtc = ahoraUtc.AddMinutes(minutos);
        UltimoError = Recortar(error);
    }

    public bool PuedeIntentar(DateTime ahoraUtc) => EstaActiva && (ProximoIntentoUtc is null || ProximoIntentoUtc <= ahoraUtc);

    public bool Toca(DateTime ahoraUtc, TimeSpan intervalo)
        => SyncToken is null || CambioRemotoPendiente || UltimaSincronizacionUtc is null
           || ahoraUtc - UltimaSincronizacionUtc >= intervalo;

    public void RegistrarCanal(string canalId, DateTime expiraUtc)
    {
        CanalId = canalId;
        CanalExpiraUtc = expiraUtc;
    }

    public void QuitarCanal()
    {
        CanalId = null;
        CanalExpiraUtc = null;
    }

    private static DateTime? Utc(DateTime? v) => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null;
    private static string? Recortar(string? s) => s is { Length: > 300 } ? s[..300] : s;
}

public enum EstadoCuentaGoogle
{
    Activa = 1,
    Revocada = 2,
}
