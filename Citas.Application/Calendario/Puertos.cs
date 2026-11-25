namespace Citas.Application.Calendario;

/// <summary>Cita tal como se publica en el calendario del empleado.</summary>
public sealed record EventoCalendario(
    string Titulo,
    string? Descripcion,
    DateTime InicioUtc,
    DateTime FinUtc,
    int IdCita);

/// <summary>
/// Evento del calendario del empleado que ocupa tiempo. Los eventos con hora traen instantes
/// UTC; los de día completo traen las fechas (Kind sin especificar) y se pasan a UTC con la
/// zona horaria del negocio.
/// </summary>
public sealed record EventoOcupado(
    string Id,
    DateTime Inicio,
    DateTime Fin,
    bool DiaCompleto,
    bool Cancelado,
    bool OcupaTiempo,
    bool CreadoPorCitas);

public sealed record CambiosCalendario(IReadOnlyList<EventoOcupado> Eventos, string? SyncToken, bool CargaCompleta);

/// <summary>Google ya no acepta el refresh token (permiso retirado o vencido): hay que reconectar.</summary>
public class CredencialesRevocadasException(string mensaje) : Exception(mensaje);

/// <summary>El token de sincronización incremental ya no vale: se hace una carga completa.</summary>
public class SyncTokenVencidoException() : Exception("El token de sincronización venció.");

/// <summary>
/// Puerto hacia el calendario externo (hoy Google Calendar). La aplicación solo conoce este
/// contrato; cambiar de proveedor (o probar sin red) es cambiar la implementación.
/// </summary>
public interface ICalendarioExterno
{
    /// <summary>Crea el evento con ese id o, si ya existe, lo actualiza (idempotente: sirve para reintentar).</summary>
    Task PublicarEventoAsync(string refreshToken, string calendarioId, string eventoId, EventoCalendario evento, CancellationToken ct = default);

    /// <summary>Borra el evento; si ya no existe no es un error.</summary>
    Task EliminarEventoAsync(string refreshToken, string calendarioId, string eventoId, CancellationToken ct = default);

    /// <summary>
    /// Eventos que cambiaron. Con <paramref name="syncToken"/> trae solo los cambios; sin él, una
    /// carga completa de [desdeUtc, hastaUtc). Lanza <see cref="SyncTokenVencidoException"/> si el token venció.
    /// </summary>
    Task<CambiosCalendario> ListarCambiosAsync(
        string refreshToken, string calendarioId, string? syncToken, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default);

    /// <summary>Pide a Google que avise (webhook) cuando cambie el calendario. Devuelve cuándo expira el canal.</summary>
    Task<DateTime> RegistrarCanalAsync(string refreshToken, string calendarioId, string canalId, string urlWebhook, string token, CancellationToken ct = default);
}

public sealed record AutorizacionGoogle(string RefreshToken, string Email);

/// <summary>Flujo OAuth 2.0 (código de autorización, acceso sin conexión) con Google.</summary>
public interface IGoogleOAuth
{
    string UrlAutorizacion(string state);
    Task<AutorizacionGoogle> CanjearCodigoAsync(string codigo, CancellationToken ct = default);
    Task RevocarAsync(string refreshToken, CancellationToken ct = default);
}

/// <summary>Cifrado de secretos en reposo (el refresh token de Google nunca se guarda en claro).</summary>
public interface ICifradorSecretos
{
    string Cifrar(string textoPlano);
    string Descifrar(string textoCifrado);
}

/// <summary>
/// Corre una acción como si la petición fuera de ese tenant: lo necesitan el proceso en segundo
/// plano y los endpoints sin token (callback de Google y webhook), que saben la empresa por otra vía.
/// </summary>
public interface IEjecutorPorTenant
{
    Task EjecutarAsync<TServicio>(string tenant, Func<TServicio, CancellationToken, Task> accion, CancellationToken ct = default)
        where TServicio : notnull;
}

/// <summary>Configuración de la integración con Google. Sin ClientId la integración queda apagada.</summary>
public sealed class OpcionesGoogle
{
    public string? ClientId { get; init; }
    public string? ClientSecret { get; init; }
    public string? RedirectUri { get; init; }

    /// <summary>Clave AES-256 en base64 (32 bytes) para cifrar el refresh token y firmar el "state" de OAuth.</summary>
    public string? ClaveCifradoBase64 { get; init; }

    /// <summary>URL pública del webhook (https). Opcional: sin ella se sincroniza solo por intervalo.</summary>
    public string? UrlWebhook { get; init; }

    /// <summary>A dónde se devuelve al usuario tras conectar (esquema de la app o URL web).</summary>
    public string RetornoApp { get; init; } = "natural://agenda/google";

    public int IntervaloEntranteSegundos { get; init; } = 300;
    public int FrecuenciaSalidaSegundos { get; init; } = 30;
    public int DiasHaciaAdelante { get; init; } = 90;

    public bool Habilitado => !string.IsNullOrWhiteSpace(ClientId);
}
