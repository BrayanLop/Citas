using System.Net;
using System.Text;
using System.Text.Json;
using Citas.Application.Calendario;
using Citas.Domain.Common;
using Citas.Infrastructure.Calendario;
using Citas.Tests.Soporte;
using Microsoft.Extensions.Caching.Memory;

namespace Citas.Tests.Calendario;

/// <summary>Google de mentira: responde por URL y guarda cada petición para revisarla.</summary>
public sealed class GoogleFalso : HttpMessageHandler
{
    public sealed record Peticion(HttpMethod Metodo, Uri Url, string Cuerpo, string? Autorizacion);

    public List<Peticion> Peticiones { get; } = [];

    /// <summary>Respuesta del endpoint de tokens.</summary>
    public Func<Peticion, HttpResponseMessage> Token { get; set; }
        = _ => Json(HttpStatusCode.OK, """{"access_token":"at-1","expires_in":3600}""");

    /// <summary>Respuesta de la API de Calendar (y de revoke).</summary>
    public Func<Peticion, HttpResponseMessage> Api { get; set; } = _ => Json(HttpStatusCode.OK, "{}");

    public IEnumerable<Peticion> LlamadasAToken => Peticiones.Where(p => p.Url.Host == "oauth2.googleapis.com" && p.Url.AbsolutePath == "/token");
    public IEnumerable<Peticion> LlamadasACalendar => Peticiones.Where(p => p.Url.Host == "www.googleapis.com");

    public static HttpResponseMessage Json(HttpStatusCode codigo, string json)
        => new(codigo) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var cuerpo = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
        var p = new Peticion(request.Method, request.RequestUri!, cuerpo, request.Headers.Authorization?.ToString());
        Peticiones.Add(p);
        return request.RequestUri!.AbsolutePath == "/token" && request.RequestUri.Host == "oauth2.googleapis.com" ? Token(p) : Api(p);
    }
}

public class GoogleCalendarHttpTests
{
    private readonly GoogleFalso _google = new();
    private readonly GoogleCalendarHttp _http;
    private static readonly OpcionesGoogle Opciones = new() { ClientId = "cliente-id", ClientSecret = "secreto", RedirectUri = "https://citas.example/Google/Callback" };

    public GoogleCalendarHttpTests()
        => _http = new GoogleCalendarHttp(new HttpClient(_google), Opciones, new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);

    private static EventoCalendario Evento(int idCita = 7)
        => new("Corte · ana", "Cita agendada con Natural.", new DateTime(2026, 11, 30, 15, 0, 0, DateTimeKind.Utc), new DateTime(2026, 11, 30, 15, 30, 0, DateTimeKind.Utc), idCita);

    private static string IdToken(string email)
    {
        static string B64(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{B64("{}")}.{B64($$"""{"email":"{{email}}"}""")}.firma";
    }

    // ---- OAuth ----

    [Fact]
    public void La_url_de_autorizacion_pide_acceso_sin_conexion_y_solo_el_permiso_de_eventos()
    {
        var url = new Uri(_http.UrlAutorizacion("estado-firmado"));
        var q = System.Web.HttpUtility.ParseQueryString(url.Query);

        Assert.Equal("accounts.google.com", url.Host);
        Assert.Equal("cliente-id", q["client_id"]);
        Assert.Equal("https://citas.example/Google/Callback", q["redirect_uri"]);
        Assert.Equal("code", q["response_type"]);
        Assert.Equal("offline", q["access_type"]);
        Assert.Equal("consent", q["prompt"]);
        Assert.Equal("estado-firmado", q["state"]);
        Assert.Equal("openid email https://www.googleapis.com/auth/calendar.events", q["scope"]);
    }

    [Fact]
    public async Task Canjear_el_codigo_devuelve_el_refresh_token_y_el_correo_del_id_token()
    {
        _google.Token = _ => GoogleFalso.Json(HttpStatusCode.OK, JsonSerializer.Serialize(new
        {
            access_token = "at", refresh_token = "r-1", expires_in = 3600,
            scope = "openid email https://www.googleapis.com/auth/calendar.events", id_token = IdToken("barbero@gmail.com"),
        }));

        var r = await _http.CanjearCodigoAsync("el-codigo");

        Assert.Equal(new AutorizacionGoogle("r-1", "barbero@gmail.com"), r);
        var cuerpo = _google.LlamadasAToken.Single().Cuerpo;
        Assert.Contains("code=el-codigo", cuerpo);
        Assert.Contains("grant_type=authorization_code", cuerpo);
    }

    [Fact]
    public async Task Si_google_no_entrega_refresh_token_se_explica_que_hay_que_volver_a_dar_el_permiso()
    {
        _google.Token = _ => GoogleFalso.Json(HttpStatusCode.OK, """{"access_token":"at","scope":"https://www.googleapis.com/auth/calendar.events"}""");

        var e = await Assert.ThrowsAsync<DomainException>(() => _http.CanjearCodigoAsync("c"));
        Assert.Contains("acceso permanente", e.Message);
    }

    [Fact]
    public async Task Si_el_usuario_no_acepto_el_permiso_del_calendario_se_rechaza()
    {
        _google.Token = _ => GoogleFalso.Json(HttpStatusCode.OK, """{"refresh_token":"r","scope":"openid email"}""");

        var e = await Assert.ThrowsAsync<DomainException>(() => _http.CanjearCodigoAsync("c"));
        Assert.Contains("permiso", e.Message);
    }

    [Fact]
    public async Task Un_rechazo_de_google_al_canjear_se_traduce_a_un_mensaje_para_el_usuario()
    {
        _google.Token = _ => GoogleFalso.Json(HttpStatusCode.BadRequest, """{"error":"invalid_grant"}""");

        await Assert.ThrowsAsync<DomainException>(() => _http.CanjearCodigoAsync("c"));
    }

    [Fact]
    public async Task Revocar_llama_al_endpoint_de_revocacion_con_el_token()
    {
        await _http.RevocarAsync("r-1");

        var p = _google.Peticiones.Single();
        Assert.Equal("oauth2.googleapis.com", p.Url.Host);
        Assert.Equal("/revoke", p.Url.AbsolutePath);
        Assert.Contains("token=r-1", p.Cuerpo);
    }

    // ---- Publicar / eliminar ----

    [Fact]
    public async Task Publicar_crea_el_evento_con_id_propio_horas_en_utc_y_la_marca_de_citas()
    {
        await _http.PublicarEventoAsync("r-1", "primary", "citasabc", Evento(7));

        var p = _google.LlamadasACalendar.Single();
        Assert.Equal(HttpMethod.Post, p.Metodo);
        Assert.Equal("/calendar/v3/calendars/primary/events", p.Url.AbsolutePath);
        Assert.Equal("Bearer at-1", p.Autorizacion);
        using var cuerpo = JsonDocument.Parse(p.Cuerpo);
        var raiz = cuerpo.RootElement;
        Assert.Equal("citasabc", raiz.GetProperty("id").GetString());
        Assert.Equal("Corte · ana", raiz.GetProperty("summary").GetString());
        Assert.Equal("2026-11-30T15:00:00Z", raiz.GetProperty("start").GetProperty("dateTime").GetString());
        Assert.Equal("2026-11-30T15:30:00Z", raiz.GetProperty("end").GetProperty("dateTime").GetString());
        Assert.Equal("7", raiz.GetProperty("extendedProperties").GetProperty("private").GetProperty("citasId").GetString());
    }

    [Fact]
    public async Task Publicar_un_evento_que_ya_existe_lo_actualiza_en_vez_de_fallar()
    {
        _google.Api = p => p.Metodo == HttpMethod.Post
            ? GoogleFalso.Json(HttpStatusCode.Conflict, """{"error":{"code":409}}""")
            : GoogleFalso.Json(HttpStatusCode.OK, "{}");

        await _http.PublicarEventoAsync("r-1", "primary", "citasabc", Evento());

        var metodos = _google.LlamadasACalendar.Select(p => p.Metodo).ToList();
        Assert.Equal([HttpMethod.Post, HttpMethod.Put], metodos);
        Assert.EndsWith("/events/citasabc", _google.LlamadasACalendar.Last().Url.AbsolutePath);
    }

    [Fact]
    public async Task Un_error_de_google_al_publicar_se_propaga_para_reintentar()
    {
        _google.Api = _ => GoogleFalso.Json(HttpStatusCode.ServiceUnavailable, "caído");

        var e = await Assert.ThrowsAsync<HttpRequestException>(() => _http.PublicarEventoAsync("r-1", "primary", "citasabc", Evento()));
        Assert.Contains("503", e.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.NoContent)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Gone)]
    public async Task Eliminar_un_evento_que_ya_no_existe_no_es_un_error(HttpStatusCode respuesta)
    {
        _google.Api = _ => GoogleFalso.Json(respuesta, "");

        await _http.EliminarEventoAsync("r-1", "primary", "citasabc");

        Assert.Equal(HttpMethod.Delete, _google.LlamadasACalendar.Single().Metodo);
    }

    [Fact]
    public async Task Eliminar_con_error_de_servidor_se_propaga()
    {
        _google.Api = _ => GoogleFalso.Json(HttpStatusCode.InternalServerError, "");

        await Assert.ThrowsAsync<HttpRequestException>(() => _http.EliminarEventoAsync("r-1", "primary", "citasabc"));
    }

    // ---- Tokens de acceso ----

    [Fact]
    public async Task El_token_de_acceso_se_reutiliza_entre_llamadas()
    {
        await _http.EliminarEventoAsync("r-1", "primary", "a");
        await _http.EliminarEventoAsync("r-1", "primary", "b");
        await _http.EliminarEventoAsync("r-1", "primary", "c");

        Assert.Single(_google.LlamadasAToken);
        Assert.Equal(3, _google.LlamadasACalendar.Count());
    }

    [Fact]
    public async Task Cada_empleado_usa_su_propio_token_de_acceso()
    {
        await _http.EliminarEventoAsync("r-1", "primary", "a");
        await _http.EliminarEventoAsync("r-2", "primary", "a");

        Assert.Equal(2, _google.LlamadasAToken.Count());
    }

    [Fact]
    public async Task Invalid_grant_al_renovar_el_token_significa_permiso_revocado()
    {
        _google.Token = _ => GoogleFalso.Json(HttpStatusCode.BadRequest, """{"error":"invalid_grant","error_description":"Token has been expired or revoked."}""");

        await Assert.ThrowsAsync<CredencialesRevocadasException>(() => _http.EliminarEventoAsync("r-1", "primary", "a"));
    }

    [Fact]
    public async Task Un_error_de_servidor_al_renovar_el_token_no_se_confunde_con_una_revocacion()
    {
        _google.Token = _ => GoogleFalso.Json(HttpStatusCode.ServiceUnavailable, "");

        await Assert.ThrowsAsync<HttpRequestException>(() => _http.EliminarEventoAsync("r-1", "primary", "a"));
    }

    [Fact]
    public async Task Un_401_pide_un_token_nuevo_una_vez_y_reintenta()
    {
        var llamadas = 0;
        var tokens = 0;
        _google.Token = _ => GoogleFalso.Json(HttpStatusCode.OK, $$"""{"access_token":"at-{{++tokens}}","expires_in":3600}""");
        _google.Api = _ => ++llamadas == 1 ? GoogleFalso.Json(HttpStatusCode.Unauthorized, "") : GoogleFalso.Json(HttpStatusCode.NoContent, "");

        await _http.EliminarEventoAsync("r-1", "primary", "a");

        Assert.Equal(2, _google.LlamadasAToken.Count());
        Assert.Equal("Bearer at-2", _google.LlamadasACalendar.Last().Autorizacion);
    }

    [Fact]
    public async Task Si_el_401_persiste_con_token_nuevo_el_permiso_esta_revocado()
    {
        _google.Api = _ => GoogleFalso.Json(HttpStatusCode.Unauthorized, "");

        await Assert.ThrowsAsync<CredencialesRevocadasException>(() => _http.EliminarEventoAsync("r-1", "primary", "a"));
    }

    // ---- Leer cambios ----

    private const string PaginaUno = """
        {
          "nextPageToken": "pag-2",
          "items": [
            {"id":"normal","status":"confirmed","start":{"dateTime":"2026-11-30T10:00:00-05:00"},"end":{"dateTime":"2026-11-30T11:00:00-05:00"}},
            {"id":"dia","status":"confirmed","start":{"date":"2026-12-01"},"end":{"date":"2026-12-02"}},
            {"id":"libre","status":"confirmed","transparency":"transparent","start":{"dateTime":"2026-11-30T12:00:00-05:00"},"end":{"dateTime":"2026-11-30T13:00:00-05:00"}}
          ]
        }
        """;

    private const string PaginaDos = """
        {
          "nextSyncToken": "sync-9",
          "items": [
            {"id":"borrado","status":"cancelled"},
            {"id":"nuestro","status":"confirmed","start":{"dateTime":"2026-11-30T14:00:00Z"},"end":{"dateTime":"2026-11-30T14:30:00Z"},"extendedProperties":{"private":{"citasId":"7"}}},
            {"id":"rechazado","status":"confirmed","start":{"dateTime":"2026-11-30T15:00:00Z"},"end":{"dateTime":"2026-11-30T16:00:00Z"},"attendees":[{"email":"x@y.z","self":true,"responseStatus":"declined"}]},
            {"id":"ubicacion","status":"confirmed","eventType":"workingLocation","start":{"date":"2026-12-03"},"end":{"date":"2026-12-04"}},
            {"id":"fuera","status":"confirmed","eventType":"outOfOffice","start":{"dateTime":"2026-12-05T09:00:00Z"},"end":{"dateTime":"2026-12-05T17:00:00Z"}}
          ]
        }
        """;

    [Fact]
    public async Task Listar_cambios_sigue_las_paginas_y_entiende_cada_tipo_de_evento()
    {
        _google.Api = p => p.Url.Query.Contains("pageToken=pag-2")
            ? GoogleFalso.Json(HttpStatusCode.OK, PaginaDos)
            : GoogleFalso.Json(HttpStatusCode.OK, PaginaUno);
        var desde = new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc);

        var r = await _http.ListarCambiosAsync("r-1", "primary", null, desde, desde.AddDays(90));

        Assert.True(r.CargaCompleta);
        Assert.Equal("sync-9", r.SyncToken);
        Assert.Equal(["normal", "dia", "libre", "borrado", "nuestro", "rechazado", "ubicacion", "fuera"], r.Eventos.Select(e => e.Id));

        var normal = r.Eventos.Single(e => e.Id == "normal");
        Assert.Equal(new DateTime(2026, 11, 30, 15, 0, 0), normal.Inicio);            // 10:00 -05:00 = 15:00Z
        Assert.True(normal.OcupaTiempo);
        Assert.False(normal.DiaCompleto);

        var dia = r.Eventos.Single(e => e.Id == "dia");
        Assert.True(dia.DiaCompleto);
        Assert.Equal(new DateTime(2026, 12, 1), dia.Inicio);
        Assert.Equal(new DateTime(2026, 12, 2), dia.Fin);

        Assert.False(r.Eventos.Single(e => e.Id == "libre").OcupaTiempo);
        Assert.True(r.Eventos.Single(e => e.Id == "borrado").Cancelado);
        Assert.True(r.Eventos.Single(e => e.Id == "nuestro").CreadoPorCitas);
        Assert.False(r.Eventos.Single(e => e.Id == "rechazado").OcupaTiempo);          // lo rechazó el empleado
        Assert.False(r.Eventos.Single(e => e.Id == "ubicacion").OcupaTiempo);          // ubicación de trabajo
        Assert.True(r.Eventos.Single(e => e.Id == "fuera").OcupaTiempo);               // fuera de oficina sí ocupa
    }

    [Fact]
    public async Task La_carga_completa_pide_una_ventana_de_tiempo_y_no_un_sync_token()
    {
        var desde = new DateTime(2026, 11, 24, 0, 0, 0, DateTimeKind.Utc);

        await _http.ListarCambiosAsync("r-1", "primary", null, desde, desde.AddDays(90));

        var q = _google.LlamadasACalendar.Single().Url.Query;
        Assert.Contains("singleEvents=true", q);
        Assert.Contains("timeMin=2026-11-24T00%3A00%3A00Z", q);
        Assert.Contains("timeMax=", q);
        Assert.DoesNotContain("syncToken", q);
    }

    [Fact]
    public async Task La_lectura_incremental_usa_el_sync_token_y_pide_los_borrados()
    {
        await _http.ListarCambiosAsync("r-1", "primary", "sync-1", default, default);

        var q = _google.LlamadasACalendar.Single().Url.Query;
        Assert.Contains("syncToken=sync-1", q);
        Assert.Contains("showDeleted=true", q);
        Assert.DoesNotContain("timeMin", q);   // Google no permite combinarlos
    }

    [Fact]
    public async Task Un_410_con_sync_token_significa_que_hay_que_recargar_todo()
    {
        _google.Api = _ => GoogleFalso.Json(HttpStatusCode.Gone, """{"error":{"code":410}}""");

        await Assert.ThrowsAsync<SyncTokenVencidoException>(() => _http.ListarCambiosAsync("r-1", "primary", "viejo", default, default));
    }

    [Fact]
    public async Task Un_410_sin_sync_token_es_un_error_comun()
    {
        _google.Api = _ => GoogleFalso.Json(HttpStatusCode.Gone, "");

        await Assert.ThrowsAsync<HttpRequestException>(() => _http.ListarCambiosAsync("r-1", "primary", null, DateTime.UtcNow, DateTime.UtcNow.AddDays(1)));
    }

    // ---- Canal de avisos ----

    [Fact]
    public async Task Registrar_el_canal_devuelve_cuando_expira()
    {
        var expira = new DateTimeOffset(2026, 12, 2, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();
        _google.Api = _ => GoogleFalso.Json(HttpStatusCode.OK, $$"""{"id":"canal-1","expiration":"{{expira}}"}""");

        var r = await _http.RegistrarCanalAsync("r-1", "primary", "canal-1", "https://citas.example/Google/Notificaciones", "tok");

        Assert.Equal(new DateTime(2026, 12, 2, 0, 0, 0, DateTimeKind.Utc), r);
        var p = _google.LlamadasACalendar.Single();
        Assert.EndsWith("/events/watch", p.Url.AbsolutePath);
        using var cuerpo = JsonDocument.Parse(p.Cuerpo);
        Assert.Equal("web_hook", cuerpo.RootElement.GetProperty("type").GetString());
        Assert.Equal("tok", cuerpo.RootElement.GetProperty("token").GetString());
    }
}
