using Citas.Application.Acceso.Abstractions;
using Citas.Application.Agendamiento;
using Citas.Application.Common;
using Citas.Domain.Calendario;
using Citas.Domain.Common;

namespace Citas.Application.Calendario;

/// <summary>Estado de la conexión de un empleado con su Google Calendar (sin ningún secreto).</summary>
public sealed class EstadoGoogleDto
{
    /// <summary>La integración está configurada en el servidor.</summary>
    public bool Habilitado { get; init; }

    public bool Conectada { get; init; }

    /// <summary>"Activa" o "Revocada" (hay que reconectar). Null si nunca se conectó.</summary>
    public string? Estado { get; init; }

    public string? Email { get; init; }
    public DateTime? UltimaSincronizacionUtc { get; init; }

    /// <summary>Último problema al sincronizar (si lo hubo).</summary>
    public string? Error { get; init; }
}

public sealed class UrlConexionGoogleDto
{
    public required string Url { get; init; }
}

public interface IConexionGoogleAppService
{
    Task<UrlConexionGoogleDto> ObtenerUrlConexionAsync(CancellationToken ct = default);
    Task<EstadoGoogleDto> EstadoAsync(int? idEmpleado = null, CancellationToken ct = default);
    Task DesconectarAsync(CancellationToken ct = default);

    /// <summary>Cierra el flujo OAuth: canjea el código, cifra el refresh token y guarda la cuenta.</summary>
    Task CompletarConexionAsync(int idEmpleado, string codigo, CancellationToken ct = default);
}

// Cada empleado conecta SU calendario (el permiso lo da él, con su cuenta de Google): no hay
// forma de conectar el de otra persona. El administrador solo puede consultar quién está conectado.
public class ConexionGoogleAppService : IConexionGoogleAppService
{
    private readonly ICalendarioRepositorio _repo;
    private readonly IGoogleOAuth _oauth;
    private readonly ICifradorSecretos _cifrador;
    private readonly IUnitOfWork _uow;
    private readonly IUsuarioActual _actual;
    private readonly FirmasGoogle _firmas;
    private readonly OpcionesGoogle _opciones;
    private readonly RelojAgenda _reloj;

    public ConexionGoogleAppService(
        ICalendarioRepositorio repo,
        IGoogleOAuth oauth,
        ICifradorSecretos cifrador,
        IUnitOfWork uow,
        IUsuarioActual actual,
        FirmasGoogle firmas,
        OpcionesGoogle opciones,
        RelojAgenda reloj)
    {
        _repo = repo;
        _oauth = oauth;
        _cifrador = cifrador;
        _uow = uow;
        _actual = actual;
        _firmas = firmas;
        _opciones = opciones;
        _reloj = reloj;
    }

    public Task<UrlConexionGoogleDto> ObtenerUrlConexionAsync(CancellationToken ct = default)
    {
        ExigirHabilitado();
        var tenant = _actual.Tenant ?? throw new ForbiddenAppException("No hay una empresa en la sesión.");
        var empleado = _actual.UsuarioId ?? throw new ForbiddenAppException("El usuario no está dado de alta en esta empresa.");

        var estado = _firmas.CrearEstado(tenant, empleado, _reloj.AhoraUtc);
        return Task.FromResult(new UrlConexionGoogleDto { Url = _oauth.UrlAutorizacion(estado) });
    }

    public async Task<EstadoGoogleDto> EstadoAsync(int? idEmpleado = null, CancellationToken ct = default)
    {
        var propio = _actual.UsuarioId ?? throw new ForbiddenAppException("El usuario no está dado de alta en esta empresa.");
        var consultado = idEmpleado ?? propio;
        if (consultado != propio && !_actual.EsAdministrador)
            throw new ForbiddenAppException("Solo un administrador puede ver la conexión de otra persona.");

        if (!_opciones.Habilitado)
            return new EstadoGoogleDto { Habilitado = false };

        var cuenta = await _repo.ObtenerCuentaAsync(consultado, ct);
        return cuenta is null
            ? new EstadoGoogleDto { Habilitado = true, Conectada = false }
            : new EstadoGoogleDto
            {
                Habilitado = true,
                Conectada = cuenta.EstaActiva,
                Estado = cuenta.Estado.ToString(),
                Email = cuenta.Email,
                UltimaSincronizacionUtc = cuenta.UltimaSincronizacionUtc,
                Error = cuenta.UltimoError,
            };
    }

    public async Task CompletarConexionAsync(int idEmpleado, string codigo, CancellationToken ct = default)
    {
        ExigirHabilitado();
        var autorizacion = await _oauth.CanjearCodigoAsync(codigo, ct);
        var cifrado = _cifrador.Cifrar(autorizacion.RefreshToken);

        var cuenta = await _repo.ObtenerCuentaAsync(idEmpleado, ct);
        if (cuenta is null)
        {
            await _repo.AgregarCuentaAsync(CuentaGoogle.Conectar(idEmpleado, autorizacion.Email, cifrado, _reloj.AhoraUtc), ct);
        }
        else
        {
            // Reconexión (o cambio de cuenta de Google): lo importado de la anterior ya no vale.
            await _repo.EliminarBloqueosDeGoogleAsync(idEmpleado, null, ct);
            cuenta.Reconectar(autorizacion.Email, cifrado, _reloj.AhoraUtc);
            _repo.ActualizarCuenta(cuenta);
        }
        await _uow.GuardarCambiosAsync(ct);
    }

    public async Task DesconectarAsync(CancellationToken ct = default)
    {
        var empleado = _actual.UsuarioId ?? throw new ForbiddenAppException("El usuario no está dado de alta en esta empresa.");
        var cuenta = await _repo.ObtenerCuentaAsync(empleado, ct);
        if (cuenta is null) return;

        // Se avisa a Google (mejor esfuerzo): si falla, igual se borra lo nuestro.
        try { await _oauth.RevocarAsync(_cifrador.Descifrar(cuenta.RefreshTokenCifrado), ct); }
        catch (Exception e) when (e is not OperationCanceledException) { }

        await _repo.EliminarBloqueosDeGoogleAsync(empleado, null, ct);
        await _repo.EliminarCuentaAsync(empleado, ct);
        await _uow.GuardarCambiosAsync(ct);
    }

    private void ExigirHabilitado()
    {
        if (!_opciones.Habilitado)
            throw new ConflictException("La integración con Google Calendar no está configurada en este servidor.");
    }
}
