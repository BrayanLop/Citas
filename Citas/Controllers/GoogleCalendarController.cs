using Citas.Application.Agendamiento;
using Citas.Application.Calendario;
using Citas.Autorizacion;
using Citas.Domain.Calendario;
using Citas.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Citas.Controllers;

// Conexión de cada empleado con SU Google Calendar. Los endpoints con sesión (estado, conectar,
// desconectar) son del personal. El callback de Google y el webhook no traen token: se protegen
// con el "state" firmado (vence en 15 min) y con el token del canal, respectivamente.
[ApiController]
[Route("[controller]")]
public class GoogleController : ControllerBase
{
    private readonly IConexionGoogleAppService _conexion;
    private readonly IEjecutorPorTenant _ejecutor;
    private readonly ICalendarioRepositorio _repo;
    private readonly FirmasGoogle _firmas;
    private readonly OpcionesGoogle _opciones;
    private readonly RelojAgenda _reloj;
    private readonly ILogger<GoogleController> _log;

    public GoogleController(
        IConexionGoogleAppService conexion,
        IEjecutorPorTenant ejecutor,
        ICalendarioRepositorio repo,
        FirmasGoogle firmas,
        OpcionesGoogle opciones,
        RelojAgenda reloj,
        ILogger<GoogleController> log)
    {
        _conexion = conexion;
        _ejecutor = ejecutor;
        _repo = repo;
        _firmas = firmas;
        _opciones = opciones;
        _reloj = reloj;
        _log = log;
    }

    /// <summary>Estado de la conexión del empleado (la propia; un administrador puede pedir la de otro).</summary>
    [Authorize(Policy = Politicas.Personal)]
    [HttpGet("Estado")]
    public async Task<ActionResult<EstadoGoogleDto>> Estado([FromQuery] int? empleadoId, CancellationToken ct)
        => Ok(await _conexion.EstadoAsync(empleadoId, ct));

    /// <summary>URL de Google donde el empleado da el permiso. La app la abre en el navegador.</summary>
    [Authorize(Policy = Politicas.Personal)]
    [HttpPost("Conexion")]
    public async Task<ActionResult<UrlConexionGoogleDto>> Conectar(CancellationToken ct)
        => Ok(await _conexion.ObtenerUrlConexionAsync(ct));

    /// <summary>Desconecta: avisa a Google, borra el permiso guardado y los bloqueos importados.</summary>
    [Authorize(Policy = Politicas.Personal)]
    [HttpDelete("Conexion")]
    public async Task<IActionResult> Desconectar(CancellationToken ct)
    {
        await _conexion.DesconectarAsync(ct);
        return NoContent();
    }

    /// <summary>Destino del redirect de Google tras el permiso. Cierra la conexión y devuelve al usuario a la app.</summary>
    [AllowAnonymous]
    [HttpGet("Callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken ct)
    {
        var destino = _firmas.ValidarEstado(state, _reloj.AhoraUtc);
        if (destino is null)
            return Volver("error", "El enlace de conexión venció. Vuelve a intentarlo desde la app.");
        if (!string.IsNullOrEmpty(error) || string.IsNullOrWhiteSpace(code))
            return Volver("error", "No diste el permiso del calendario.");

        try
        {
            await _ejecutor.EjecutarAsync<IConexionGoogleAppService>(
                destino.Value.Tenant, (s, token) => s.CompletarConexionAsync(destino.Value.IdEmpleado, code, token), ct);
            return Volver("ok", null);
        }
        catch (AppException e)
        {
            return Volver("error", e.Message);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            _log.LogError(e, "Falló la conexión con Google Calendar.");
            return Volver("error", "No se pudo conectar con Google. Intenta de nuevo.");
        }
    }

    /// <summary>Aviso de Google: cambió el calendario de un empleado. Solo se anota; sincroniza el proceso de fondo.</summary>
    [AllowAnonymous]
    [HttpPost("Notificaciones")]
    public async Task<IActionResult> Notificaciones(CancellationToken ct)
    {
        var canal = Request.Headers["X-Goog-Channel-ID"].ToString();
        var token = Request.Headers["X-Goog-Channel-Token"].ToString();
        var estado = Request.Headers["X-Goog-Resource-State"].ToString();

        // Ignora avisos sin firma válida y el aviso inicial "sync". Siempre 200: Google reintenta si no.
        if (string.IsNullOrEmpty(canal) || !_firmas.TokenCanalValido(canal, token) || estado == "sync")
            return Ok();

        var cuenta = await _repo.BuscarPorCanalAsync(canal, ct);
        if (cuenta is null)
            return Ok();

        await _ejecutor.EjecutarAsync<SincronizadorCalendario>(
            cuenta.Tenant, (s, t) => s.MarcarCambioRemotoAsync(cuenta.IdEmpleado, t), ct);
        return Ok();
    }

    private RedirectResult Volver(string resultado, string? mensaje)
    {
        var url = $"{_opciones.RetornoApp}{(_opciones.RetornoApp.Contains('?') ? '&' : '?')}estado={resultado}";
        if (mensaje is not null)
            url += "&mensaje=" + Uri.EscapeDataString(mensaje);
        return Redirect(url);
    }
}
