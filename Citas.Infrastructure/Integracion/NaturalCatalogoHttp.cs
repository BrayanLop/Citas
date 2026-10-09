using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Citas.Application.Acceso.Abstractions;
using Citas.Application.Integracion;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Citas.Infrastructure.Integracion;

/// <summary>
/// Adaptador HTTP de <see cref="INaturalCatalogo"/> (cliente tipado de IHttpClientFactory).
/// Reenvía el mismo token Bearer de Natural que trajo la petición entrante y el header
/// 'empresaId' con el tenant (Natural toma la empresa del token; el header es por
/// compatibilidad). Traduce los fallos de Natural a <see cref="IntegracionNaturalException"/>.
/// </summary>
public class NaturalCatalogoHttp : INaturalCatalogo
{
    private const string RutaServicios = "Servicio/Obtener";
    private const string RutaPersonas = "Persona/Obtener";

    // camelCase, sin distinguir mayúsculas y aceptando números como texto.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IUsuarioActual _actual;
    private readonly ILogger<NaturalCatalogoHttp> _logger;

    public NaturalCatalogoHttp(
        HttpClient http,
        IHttpContextAccessor httpContext,
        IUsuarioActual actual,
        ILogger<NaturalCatalogoHttp> logger)
    {
        _http = http;
        _httpContext = httpContext;
        _actual = actual;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ServicioNatural>> ObtenerServiciosAsync(CancellationToken cancellationToken = default)
    {
        var filas = await ObtenerAsync<ServicioJson>(RutaServicios, cancellationToken);
        return filas
            .Select(s => new ServicioNatural(s.Id, s.Nombre, s.Precio ?? 0m, s.Disponible ?? false, s.EmpresaId))
            .ToList();
    }

    public async Task<IReadOnlyList<PersonaNatural>> ObtenerPersonasAsync(CancellationToken cancellationToken = default)
    {
        var filas = await ObtenerAsync<PersonaJson>(RutaPersonas, cancellationToken);
        return filas
            .Select(p => new PersonaNatural(p.Id, p.Nombre, p.Apellido, p.Email, Texto(p.Celular), p.EmpresaId))
            .ToList();
    }

    private async Task<List<T>> ObtenerAsync<T>(string ruta, CancellationToken cancellationToken)
    {
        using var peticion = new HttpRequestMessage(HttpMethod.Get, ruta);
        peticion.Headers.Authorization = TokenDeNatural();
        if (_actual.Tenant is { } tenant)
            peticion.Headers.TryAddWithoutValidation("empresaId", tenant);

        try
        {
            using var respuesta = await _http.SendAsync(peticion, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (respuesta.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _logger.LogWarning("Natural respondió {Status} en {Ruta}.", (int)respuesta.StatusCode, ruta);
                throw IntegracionNaturalException.SinPermiso(
                    $"Natural rechazó la consulta de {ruta} ({(int)respuesta.StatusCode}): el token no es válido, " +
                    "expiró o no tiene permiso. Vuelve a iniciar sesión en Natural.");
            }

            if (!respuesta.IsSuccessStatusCode)
            {
                _logger.LogWarning("Natural respondió {Status} en {Ruta}.", (int)respuesta.StatusCode, ruta);
                throw IntegracionNaturalException.NoDisponible(
                    $"Natural respondió {(int)respuesta.StatusCode} al consultar {ruta}. Inténtalo más tarde.");
            }

            return await respuesta.Content.ReadFromJsonAsync<List<T>>(Json, cancellationToken) ?? [];
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Natural no respondió a tiempo en {Ruta}.", ruta);
            throw IntegracionNaturalException.NoDisponible(
                $"Natural no respondió a tiempo al consultar {ruta}. Inténtalo más tarde.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "No se pudo conectar con Natural en {Ruta}.", ruta);
            throw IntegracionNaturalException.NoDisponible(
                $"No se pudo conectar con Natural al consultar {ruta}. Inténtalo más tarde.");
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            _logger.LogWarning(ex, "Respuesta de Natural no interpretable en {Ruta}.", ruta);
            throw IntegracionNaturalException.NoDisponible(
                $"Natural devolvió una respuesta inesperada al consultar {ruta}.");
        }
    }

    // El mismo Bearer que trajo la petición entrante (token de Natural del personal).
    private AuthenticationHeaderValue TokenDeNatural()
    {
        var cabecera = _httpContext.HttpContext?.Request.Headers.Authorization.ToString();
        if (!_actual.EsPersonal ||
            !AuthenticationHeaderValue.TryParse(cabecera, out var valor) ||
            !string.Equals(valor.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(valor.Parameter))
        {
            throw IntegracionNaturalException.SinPermiso(
                "Para consultar Natural la petición debe traer el token de Natural del personal.");
        }

        return new AuthenticationHeaderValue("Bearer", valor.Parameter);
    }

    private static string? Texto(JsonElement? valor) => valor?.ValueKind switch
    {
        JsonValueKind.String => valor.Value.GetString(),
        JsonValueKind.Number => valor.Value.GetRawText(),
        _ => null,
    };

    // Solo los campos que Citas usa; el resto de la respuesta de Natural se ignora.
    private sealed class ServicioJson
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public decimal? Precio { get; set; }
        public bool? Disponible { get; set; }
        public int? EmpresaId { get; set; }
    }

    private sealed class PersonaJson
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Apellido { get; set; }
        public string? Email { get; set; }
        // Puede llegar como texto o como número.
        public JsonElement? Celular { get; set; }
        public int? EmpresaId { get; set; }
    }
}
