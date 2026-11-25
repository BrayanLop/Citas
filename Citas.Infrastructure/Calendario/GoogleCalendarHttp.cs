using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Citas.Application.Calendario;
using Citas.Domain.Common;
using Microsoft.Extensions.Caching.Memory;

namespace Citas.Infrastructure.Calendario;

/// <summary>
/// Google Calendar y OAuth 2.0 de Google por HTTP directo (sin SDK: menos dependencias y se
/// prueba con un handler falso). Scopes mínimos para publicar citas y leer los eventos ocupados:
/// <c>calendar.events</c> (más <c>openid email</c> solo para mostrar con qué cuenta se conectó).
/// </summary>
public sealed class GoogleCalendarHttp : ICalendarioExterno, IGoogleOAuth
{
    public const string ScopeCalendario = "https://www.googleapis.com/auth/calendar.events";

    private const string UrlAutorizacionGoogle = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string UrlToken = "https://oauth2.googleapis.com/token";
    private const string UrlRevocar = "https://oauth2.googleapis.com/revoke";
    private const string UrlCalendario = "https://www.googleapis.com/calendar/v3/";

    private readonly HttpClient _http;
    private readonly OpcionesGoogle _opciones;
    private readonly IMemoryCache _cache;
    private readonly TimeProvider _tiempo;

    public GoogleCalendarHttp(HttpClient http, OpcionesGoogle opciones, IMemoryCache cache, TimeProvider tiempo)
    {
        _http = http;
        _opciones = opciones;
        _cache = cache;
        _tiempo = tiempo;
    }

    // ---- OAuth ----------------------------------------------------------------------------

    public string UrlAutorizacion(string state)
    {
        var parametros = new Dictionary<string, string>
        {
            ["client_id"] = _opciones.ClientId ?? "",
            ["redirect_uri"] = _opciones.RedirectUri ?? "",
            ["response_type"] = "code",
            ["scope"] = $"openid email {ScopeCalendario}",
            ["access_type"] = "offline",       // para recibir el refresh token
            ["prompt"] = "consent",            // y recibirlo también al reconectar
            ["include_granted_scopes"] = "true",
            ["state"] = state,
        };
        return UrlAutorizacionGoogle + "?" + string.Join('&', parametros.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
    }

    public async Task<AutorizacionGoogle> CanjearCodigoAsync(string codigo, CancellationToken ct = default)
    {
        using var respuesta = await _http.PostAsync(UrlToken, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = codigo,
            ["client_id"] = _opciones.ClientId ?? "",
            ["client_secret"] = _opciones.ClientSecret ?? "",
            ["redirect_uri"] = _opciones.RedirectUri ?? "",
            ["grant_type"] = "authorization_code",
        }), ct);

        if (!respuesta.IsSuccessStatusCode)
            throw new DomainException("Google rechazó la autorización. Vuelve a intentar la conexión.");

        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
        var raiz = json.RootElement;

        var scopes = Texto(raiz, "scope") ?? "";
        if (!scopes.Split(' ').Contains(ScopeCalendario))
            throw new DomainException("Debes aceptar el permiso para ver y editar los eventos de tu calendario.");

        var refresh = Texto(raiz, "refresh_token");
        if (string.IsNullOrEmpty(refresh))
            throw new DomainException(
                "Google no entregó acceso permanente. Quita el acceso de Natural en tu cuenta de Google y vuelve a conectar.");

        return new AutorizacionGoogle(refresh, EmailDelIdToken(Texto(raiz, "id_token")));
    }

    public async Task RevocarAsync(string refreshToken, CancellationToken ct = default)
    {
        using var respuesta = await _http.PostAsync(UrlRevocar,
            new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = refreshToken }), ct);
        // Google responde 400 si el token ya estaba revocado: para nosotros es lo mismo.
    }

    // ---- Eventos --------------------------------------------------------------------------

    public async Task PublicarEventoAsync(
        string refreshToken, string calendarioId, string eventoId, EventoCalendario evento, CancellationToken ct = default)
    {
        var cuerpo = new Dictionary<string, object?>
        {
            ["id"] = eventoId,
            ["summary"] = evento.Titulo,
            ["description"] = evento.Descripcion,
            ["start"] = new { dateTime = Rfc3339(evento.InicioUtc) },
            ["end"] = new { dateTime = Rfc3339(evento.FinUtc) },
            ["status"] = "confirmed",
            ["reminders"] = new { useDefault = true },
            // Marca para no volver a importar este evento como bloqueo.
            ["extendedProperties"] = new { @private = new Dictionary<string, string> { ["citasId"] = evento.IdCita.ToString(CultureInfo.InvariantCulture) } },
        };

        var calendario = Uri.EscapeDataString(calendarioId);
        using var creado = await EnviarAsync(refreshToken, HttpMethod.Post, $"calendars/{calendario}/events", cuerpo, ct);
        if (creado.StatusCode != HttpStatusCode.Conflict)
        {
            await AsegurarExitoAsync(creado, ct);
            return;
        }

        // Ya existía (un reintento, o una reconexión): se actualiza con lo vigente.
        using var actualizado = await EnviarAsync(
            refreshToken, HttpMethod.Put, $"calendars/{calendario}/events/{Uri.EscapeDataString(eventoId)}", cuerpo, ct);
        await AsegurarExitoAsync(actualizado, ct);
    }

    public async Task EliminarEventoAsync(string refreshToken, string calendarioId, string eventoId, CancellationToken ct = default)
    {
        using var respuesta = await EnviarAsync(
            refreshToken, HttpMethod.Delete,
            $"calendars/{Uri.EscapeDataString(calendarioId)}/events/{Uri.EscapeDataString(eventoId)}", null, ct);
        if (respuesta.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            return;   // ya no estaba
        await AsegurarExitoAsync(respuesta, ct);
    }

    public async Task<CambiosCalendario> ListarCambiosAsync(
        string refreshToken, string calendarioId, string? syncToken, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
    {
        var eventos = new List<EventoOcupado>();
        string? paginaToken = null;
        string? siguienteSync;

        do
        {
            var consulta = new List<string> { "singleEvents=true", "maxResults=250" };
            if (syncToken is not null)
            {
                consulta.Add("syncToken=" + Uri.EscapeDataString(syncToken));
                consulta.Add("showDeleted=true");
            }
            else
            {
                consulta.Add("timeMin=" + Uri.EscapeDataString(Rfc3339(desdeUtc)));
                consulta.Add("timeMax=" + Uri.EscapeDataString(Rfc3339(hastaUtc)));
            }
            if (paginaToken is not null)
                consulta.Add("pageToken=" + Uri.EscapeDataString(paginaToken));

            using var respuesta = await EnviarAsync(
                refreshToken, HttpMethod.Get, $"calendars/{Uri.EscapeDataString(calendarioId)}/events?{string.Join('&', consulta)}", null, ct);
            if (respuesta.StatusCode == HttpStatusCode.Gone && syncToken is not null)
                throw new SyncTokenVencidoException();
            await AsegurarExitoAsync(respuesta, ct);

            using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
            var raiz = json.RootElement;
            if (raiz.TryGetProperty("items", out var items))
                eventos.AddRange(items.EnumerateArray().Select(LeerEvento).OfType<EventoOcupado>());

            paginaToken = Texto(raiz, "nextPageToken");
            siguienteSync = Texto(raiz, "nextSyncToken");
        }
        while (paginaToken is not null);

        return new CambiosCalendario(eventos, siguienteSync, CargaCompleta: syncToken is null);
    }

    public async Task<DateTime> RegistrarCanalAsync(
        string refreshToken, string calendarioId, string canalId, string urlWebhook, string token, CancellationToken ct = default)
    {
        var cuerpo = new Dictionary<string, object?> { ["id"] = canalId, ["type"] = "web_hook", ["address"] = urlWebhook, ["token"] = token };
        using var respuesta = await EnviarAsync(
            refreshToken, HttpMethod.Post, $"calendars/{Uri.EscapeDataString(calendarioId)}/events/watch", cuerpo, ct);
        await AsegurarExitoAsync(respuesta, ct);

        using var json = JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync(ct));
        var expira = Texto(json.RootElement, "expiration");
        return long.TryParse(expira, out var ms)
            ? DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime
            : _tiempo.GetUtcNow().UtcDateTime.AddDays(6);   // Google no devolvió la expiración: se asume la mínima habitual
    }

    // ---- Detalles -------------------------------------------------------------------------

    private async Task<HttpResponseMessage> EnviarAsync(
        string refreshToken, HttpMethod metodo, string ruta, object? cuerpo, CancellationToken ct)
    {
        var acceso = await TokenDeAccesoAsync(refreshToken, ct);
        var peticion = new HttpRequestMessage(metodo, UrlCalendario + ruta);
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", acceso);
        if (cuerpo is not null)
            peticion.Content = JsonContent.Create(cuerpo);

        var respuesta = await _http.SendAsync(peticion, ct);
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
        {
            // El token de acceso en caché ya no sirve: se descarta y se pide uno nuevo una vez.
            _cache.Remove(ClaveCache(refreshToken));
            respuesta.Dispose();
            var reintento = new HttpRequestMessage(metodo, UrlCalendario + ruta);
            reintento.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await TokenDeAccesoAsync(refreshToken, ct));
            if (cuerpo is not null)
                reintento.Content = JsonContent.Create(cuerpo);
            respuesta = await _http.SendAsync(reintento, ct);
            if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            {
                respuesta.Dispose();
                throw new CredencialesRevocadasException("Google no aceptó las credenciales del calendario.");
            }
        }
        return respuesta;
    }

    private async Task<string> TokenDeAccesoAsync(string refreshToken, CancellationToken ct)
    {
        var clave = ClaveCache(refreshToken);
        if (_cache.TryGetValue(clave, out string? guardado) && guardado is not null)
            return guardado;

        using var respuesta = await _http.PostAsync(UrlToken, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _opciones.ClientId ?? "",
            ["client_secret"] = _opciones.ClientSecret ?? "",
            ["refresh_token"] = refreshToken,
            ["grant_type"] = "refresh_token",
        }), ct);

        var texto = await respuesta.Content.ReadAsStringAsync(ct);
        if (!respuesta.IsSuccessStatusCode)
        {
            // invalid_grant = el empleado retiró el permiso o el token venció/fue revocado.
            if (texto.Contains("invalid_grant", StringComparison.Ordinal))
                throw new CredencialesRevocadasException("Google ya no acepta el permiso (invalid_grant).");
            throw new HttpRequestException($"Google no entregó un token de acceso ({(int)respuesta.StatusCode}).");
        }

        using var json = JsonDocument.Parse(texto);
        var acceso = Texto(json.RootElement, "access_token")
            ?? throw new HttpRequestException("Google no entregó un token de acceso.");
        var segundos = json.RootElement.TryGetProperty("expires_in", out var e) && e.TryGetInt32(out var s) ? s : 3600;
        _cache.Set(clave, acceso, TimeSpan.FromSeconds(Math.Max(60, segundos - 120)));
        return acceso;
    }

    private static async Task AsegurarExitoAsync(HttpResponseMessage respuesta, CancellationToken ct)
    {
        if (respuesta.IsSuccessStatusCode) return;
        var detalle = await respuesta.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException(
            $"Google Calendar respondió {(int)respuesta.StatusCode}: {(detalle.Length > 200 ? detalle[..200] : detalle)}");
    }

    private static EventoOcupado? LeerEvento(JsonElement e)
    {
        var id = Texto(e, "id");
        if (id is null) return null;

        var cancelado = Texto(e, "status") == "cancelled";
        var creadoPorCitas = e.TryGetProperty("extendedProperties", out var ext)
            && ext.TryGetProperty("private", out var priv) && priv.TryGetProperty("citasId", out _);

        // Libre (transparente), cumpleaños, ubicación de trabajo o invitación rechazada: no ocupan tiempo.
        var tipo = Texto(e, "eventType");
        var ocupa = Texto(e, "transparency") != "transparent"
            && (tipo is null or "default" or "outOfOffice" or "focusTime")
            && !Rechazado(e);

        // Un evento cancelado puede venir sin fechas.
        if (!e.TryGetProperty("start", out var inicio) || !e.TryGetProperty("end", out var fin))
            return new EventoOcupado(id, default, default, false, cancelado, false, creadoPorCitas);

        if (inicio.TryGetProperty("date", out var diaInicio) && fin.TryGetProperty("date", out var diaFin))
        {
            return new EventoOcupado(id,
                DateTime.ParseExact(diaInicio.GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateTime.ParseExact(diaFin.GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                DiaCompleto: true, cancelado, ocupa, creadoPorCitas);
        }

        var desde = Texto(inicio, "dateTime");
        var hasta = Texto(fin, "dateTime");
        if (desde is null || hasta is null)
            return new EventoOcupado(id, default, default, false, cancelado, false, creadoPorCitas);

        return new EventoOcupado(id,
            DateTimeOffset.Parse(desde, CultureInfo.InvariantCulture).UtcDateTime,
            DateTimeOffset.Parse(hasta, CultureInfo.InvariantCulture).UtcDateTime,
            DiaCompleto: false, cancelado, ocupa, creadoPorCitas);
    }

    private static bool Rechazado(JsonElement e)
        => e.TryGetProperty("attendees", out var invitados)
           && invitados.EnumerateArray().Any(a =>
               a.TryGetProperty("self", out var propio) && propio.ValueKind == JsonValueKind.True
               && Texto(a, "responseStatus") == "declined");

    private static string? Texto(JsonElement e, string propiedad)
        => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(propiedad, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString()
            : null;

    private static string Rfc3339(DateTime utc)
        => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static string ClaveCache(string refreshToken)
        => "google-acceso:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)))[..24];

    // El id_token llega directo de Google por TLS en la respuesta del canje: basta leer su carga.
    private static string EmailDelIdToken(string? idToken)
    {
        try
        {
            var carga = idToken?.Split('.')[1];
            if (carga is null) return string.Empty;
            carga = carga.Replace('-', '+').Replace('_', '/');
            carga = carga.PadRight(carga.Length + (4 - carga.Length % 4) % 4, '=');
            using var json = JsonDocument.Parse(Convert.FromBase64String(carga));
            return Texto(json.RootElement, "email") ?? string.Empty;
        }
        catch (Exception e) when (e is FormatException or JsonException or IndexOutOfRangeException)
        {
            return string.Empty;
        }
    }
}
