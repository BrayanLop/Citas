using System.Security.Cryptography;
using System.Text.Json;
using Citas.Application.Acceso.Abstractions;
using Citas.Application.Calendario;
using Citas.Domain.Calendario;
using Citas.Domain.Common;
using Citas.Infrastructure.Calendario;
using Citas.Models;
using Citas.Tests.Soporte;

namespace Citas.Tests.Calendario;

public class CifradorTests
{
    private static OpcionesGoogle Clave(byte relleno = 7) => new() { ClaveCifradoBase64 = Convert.ToBase64String(Enumerable.Repeat(relleno, 32).ToArray()) };

    [Fact]
    public void Descifrar_devuelve_lo_que_se_cifro_incluso_con_acentos_y_simbolos()
    {
        var c = new CifradorAesGcm(Clave());
        const string token = "1//0gÑandú-ñ_üñ/+=refresh";

        Assert.Equal(token, c.Descifrar(c.Cifrar(token)));
    }

    [Fact]
    public void El_texto_cifrado_no_contiene_el_secreto_y_cada_cifrado_es_distinto()
    {
        var c = new CifradorAesGcm(Clave());

        var a = c.Cifrar("refresh-token-secreto");
        var b = c.Cifrar("refresh-token-secreto");

        Assert.DoesNotContain("secreto", a);
        Assert.NotEqual(a, b);   // nonce aleatorio por cifrado
    }

    [Fact]
    public void Si_alguien_altera_el_dato_guardado_no_se_descifra()
    {
        var c = new CifradorAesGcm(Clave());
        var bytes = Convert.FromBase64String(c.Cifrar("refresh"));
        bytes[^1] ^= 0x01;

        Assert.ThrowsAny<CryptographicException>(() => c.Descifrar(Convert.ToBase64String(bytes)));
    }

    [Fact]
    public void Con_otra_clave_no_se_puede_descifrar()
    {
        var cifrado = new CifradorAesGcm(Clave(1)).Cifrar("refresh");

        Assert.ThrowsAny<CryptographicException>(() => new CifradorAesGcm(Clave(2)).Descifrar(cifrado));
    }

    [Fact]
    public void Un_dato_demasiado_corto_se_rechaza()
        => Assert.ThrowsAny<CryptographicException>(() => new CifradorAesGcm(Clave()).Descifrar(Convert.ToBase64String(new byte[10])));

    [Fact]
    public void Una_clave_que_no_mide_32_bytes_hace_fallar_al_crear_el_cifrador()
        => Assert.Throws<InvalidOperationException>(() =>
            new CifradorAesGcm(new OpcionesGoogle { ClaveCifradoBase64 = Convert.ToBase64String(new byte[16]) }));

    [Fact]
    public void Sin_clave_configurada_no_se_puede_cifrar()
        => Assert.Throws<InvalidOperationException>(() => new CifradorAesGcm(new OpcionesGoogle()).Cifrar("x"));
}

public class FirmasGoogleTests
{
    private static readonly DateTime Ahora = new(2026, 11, 25, 15, 0, 0, DateTimeKind.Utc);

    private static FirmasGoogle Firmas(byte clave = 1)
        => new(new OpcionesGoogle { ClaveCifradoBase64 = Convert.ToBase64String(Enumerable.Repeat(clave, 32).ToArray()) });

    [Fact]
    public void El_estado_firmado_devuelve_la_empresa_y_el_empleado()
    {
        var f = Firmas();

        var estado = f.CrearEstado("empresa-9", 42, Ahora);

        Assert.Equal(("empresa-9", 42), f.ValidarEstado(estado, Ahora.AddMinutes(5)));
    }

    [Fact]
    public void El_estado_vence_a_los_15_minutos()
    {
        var f = Firmas();
        var estado = f.CrearEstado("T1", 1, Ahora);

        Assert.NotNull(f.ValidarEstado(estado, Ahora.AddMinutes(14)));
        Assert.Null(f.ValidarEstado(estado, Ahora.AddMinutes(16)));
    }

    [Fact]
    public void Un_estado_firmado_con_otra_clave_se_rechaza()
        => Assert.Null(Firmas(1).ValidarEstado(Firmas(2).CrearEstado("T1", 1, Ahora), Ahora));

    [Fact]
    public void Cambiar_el_empleado_dentro_del_estado_invalida_la_firma()
    {
        var f = Firmas();
        var partes = f.CrearEstado("T1", 1, Ahora).Split('.');
        // Un atacante reemplaza la carga por otra (otro empleado) y conserva la firma.
        var carga = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("T1|99|9999999999")).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        Assert.Null(f.ValidarEstado($"{carga}.{partes[1]}", Ahora));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("basura")]
    [InlineData("a.b.c")]
    [InlineData("%%%.%%%")]
    public void Un_estado_mal_formado_se_rechaza_sin_lanzar_excepcion(string? estado)
        => Assert.Null(Firmas().ValidarEstado(estado, Ahora));

    [Fact]
    public void El_token_del_canal_solo_vale_para_su_canal()
    {
        var f = Firmas();
        var token = f.TokenCanal("canal-1");

        Assert.True(f.TokenCanalValido("canal-1", token));
        Assert.False(f.TokenCanalValido("canal-2", token));
        Assert.False(f.TokenCanalValido("canal-1", null));
        Assert.False(f.TokenCanalValido("canal-1", ""));
        Assert.False(Firmas(2).TokenCanalValido("canal-1", token));
    }

    [Fact]
    public void Un_estado_de_oauth_no_sirve_como_token_de_canal()
    {
        var f = Firmas();
        var estado = f.CrearEstado("T1", 1, Ahora);

        Assert.False(f.TokenCanalValido(estado.Split('.')[0], estado.Split('.')[1]));
    }
}

public class ConexionGoogleAppServiceTests
{
    private readonly Escenario _e = new();
    private readonly CalendarioRepositorioMemoria _repo;
    private readonly GoogleOAuthFalso _oauth = new();
    private readonly CifradorFalso _cifrador = new();
    private readonly UsuarioActualFalso _actual = new() { Tenant = "T1", UsuarioId = Escenario.Empleado, RolNatural = RolesNatural.Trabajador };
    private readonly OpcionesGoogle _opciones = new() { ClientId = "id", ClaveCifradoBase64 = Convert.ToBase64String(new byte[32]) };
    private readonly FirmasGoogle _firmas;

    public ConexionGoogleAppServiceTests()
    {
        _repo = new CalendarioRepositorioMemoria(_e.Agenda);
        _firmas = new FirmasGoogle(_opciones);
    }

    private ConexionGoogleAppService Servicio(OpcionesGoogle? opciones = null)
        => new(_repo, _oauth, _cifrador, new UnidadTrabajoMemoria(), _actual, _firmas, opciones ?? _opciones, _e.Reloj);

    [Fact]
    public async Task La_url_de_conexion_lleva_un_state_que_identifica_empresa_y_empleado()
    {
        var url = (await Servicio().ObtenerUrlConexionAsync()).Url;

        var state = Uri.UnescapeDataString(url[(url.IndexOf("state=", StringComparison.Ordinal) + 6)..]);
        Assert.Equal(("T1", Escenario.Empleado), _firmas.ValidarEstado(state, _e.Reloj.AhoraUtc));
    }

    [Fact]
    public async Task Sin_configurar_google_conectar_da_conflicto_y_el_estado_dice_que_no_esta_habilitado()
    {
        var s = Servicio(new OpcionesGoogle());

        await Assert.ThrowsAsync<ConflictException>(() => s.ObtenerUrlConexionAsync());
        Assert.False((await s.EstadoAsync()).Habilitado);
    }

    [Fact]
    public async Task Completar_la_conexion_guarda_el_token_cifrado_y_el_correo()
    {
        await Servicio().CompletarConexionAsync(Escenario.Empleado, "codigo-de-google");

        var cuenta = Assert.Single(_repo.Cuentas);
        Assert.Equal("codigo-de-google", _oauth.CodigoRecibido);
        Assert.Equal("barbero@gmail.com", cuenta.Email);
        Assert.NotEqual("refresh-123", cuenta.RefreshTokenCifrado);
        Assert.Equal("refresh-123", _cifrador.Descifrar(cuenta.RefreshTokenCifrado));
        Assert.True(cuenta.EstaActiva);
    }

    [Fact]
    public async Task Reconectar_una_cuenta_revocada_la_deja_activa_y_borra_lo_importado_de_la_anterior()
    {
        await _repo.AgregarCuentaAsync(CuentaGoogle.Conectar(Escenario.Empleado, "viejo@gmail.com", _cifrador.Cifrar("viejo"), _e.Reloj.AhoraUtc));
        _repo.Cuentas[0].Revocar("invalid_grant");
        var bloqueo = BloqueoAgenda.Crear(Escenario.Empleado, _e.Utc(Escenario.Lunes, "10:00"), _e.Utc(Escenario.Lunes, "11:00"), "Ocupado");
        await _e.Agenda.AgregarBloqueoAsync(bloqueo);
        await _repo.AgregarVinculoBloqueoAsync(Escenario.Empleado, bloqueo.IdBloqueo, "g1");

        await Servicio().CompletarConexionAsync(Escenario.Empleado, "codigo");

        var cuenta = Assert.Single(_repo.Cuentas);
        Assert.True(cuenta.EstaActiva);
        Assert.Equal("barbero@gmail.com", cuenta.Email);
        Assert.Null(cuenta.SyncToken);
        Assert.Empty(_e.Agenda.Bloqueos);
    }

    [Fact]
    public async Task El_estado_no_expone_ningun_secreto()
    {
        await Servicio().CompletarConexionAsync(Escenario.Empleado, "codigo");

        var estado = await Servicio().EstadoAsync();

        var json = JsonSerializer.Serialize(estado);
        Assert.DoesNotContain("refresh", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("enc:", json);
        Assert.True(estado.Habilitado);
        Assert.True(estado.Conectada);
        Assert.Equal("barbero@gmail.com", estado.Email);
    }

    [Fact]
    public async Task Una_cuenta_revocada_se_ve_como_no_conectada_para_pedir_reconectar()
    {
        await _repo.AgregarCuentaAsync(CuentaGoogle.Conectar(Escenario.Empleado, "a@b.c", _cifrador.Cifrar("x"), _e.Reloj.AhoraUtc));
        _repo.Cuentas[0].Revocar("invalid_grant");

        var estado = await Servicio().EstadoAsync();

        Assert.False(estado.Conectada);
        Assert.Equal("Revocada", estado.Estado);
    }

    [Fact]
    public async Task Un_trabajador_no_ve_la_conexion_de_otro_pero_un_administrador_si()
    {
        await Assert.ThrowsAsync<ForbiddenAppException>(() => Servicio().EstadoAsync(12));

        _actual.RolNatural = RolesNatural.Administrador;
        Assert.False((await Servicio().EstadoAsync(12)).Conectada);
    }

    [Fact]
    public async Task Sin_usuario_de_citas_no_se_puede_conectar()
    {
        _actual.UsuarioId = null;

        await Assert.ThrowsAsync<ForbiddenAppException>(() => Servicio().ObtenerUrlConexionAsync());
    }

    [Fact]
    public async Task Desconectar_avisa_a_google_y_borra_cuenta_vinculos_y_bloqueos_importados()
    {
        await Servicio().CompletarConexionAsync(Escenario.Empleado, "codigo");
        await _repo.AgregarVinculoCitaAsync(Escenario.Empleado, new VinculoCita(1, "citasx", "h"));
        var bloqueo = BloqueoAgenda.Crear(Escenario.Empleado, _e.Utc(Escenario.Lunes, "10:00"), _e.Utc(Escenario.Lunes, "11:00"), "Ocupado");
        await _e.Agenda.AgregarBloqueoAsync(bloqueo);
        await _repo.AgregarVinculoBloqueoAsync(Escenario.Empleado, bloqueo.IdBloqueo, "g1");

        await Servicio().DesconectarAsync();

        Assert.Equal("refresh-123", Assert.Single(_oauth.Revocados));
        Assert.Empty(_repo.Cuentas);
        Assert.Empty(_repo.VinculosCita);
        Assert.Empty(_e.Agenda.Bloqueos);
    }

    [Fact]
    public async Task Desconectar_borra_lo_nuestro_aunque_google_no_responda()
    {
        await Servicio().CompletarConexionAsync(Escenario.Empleado, "codigo");
        _oauth.FallarAlRevocar = true;

        await Servicio().DesconectarAsync();

        Assert.Empty(_repo.Cuentas);
    }

    [Fact]
    public async Task Desconectar_sin_cuenta_no_es_un_error()
        => await Servicio().DesconectarAsync();
}

public class CuentaGoogleTests
{
    private static readonly DateTime Ahora = new(2026, 11, 25, 15, 0, 0, DateTimeKind.Utc);

    private static CuentaGoogle Nueva() => CuentaGoogle.Conectar(10, "a@b.c", "cifrado", Ahora);

    [Fact]
    public void Una_cuenta_nueva_esta_activa_y_le_toca_leer_google()
    {
        var c = Nueva();

        Assert.True(c.EstaActiva);
        Assert.True(c.Toca(Ahora, TimeSpan.FromMinutes(5)));   // sin syncToken: falta la carga completa
        Assert.True(c.PuedeIntentar(Ahora));
    }

    [Fact]
    public void Tras_sincronizar_no_toca_hasta_que_pase_el_intervalo_o_llegue_un_aviso()
    {
        var c = Nueva();
        c.RegistrarSincronizacionEntrante("tok", Ahora);

        Assert.False(c.Toca(Ahora.AddMinutes(4), TimeSpan.FromMinutes(5)));
        Assert.True(c.Toca(Ahora.AddMinutes(5), TimeSpan.FromMinutes(5)));

        c.MarcarCambioRemoto();
        Assert.True(c.Toca(Ahora.AddMinutes(1), TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void Revocar_olvida_el_estado_de_sincronizacion_y_el_canal()
    {
        var c = Nueva();
        c.RegistrarSincronizacionEntrante("tok", Ahora);
        c.RegistrarCanal("canal", Ahora.AddDays(7));

        c.Revocar("invalid_grant");

        Assert.False(c.EstaActiva);
        Assert.Null(c.SyncToken);
        Assert.Null(c.CanalId);
        Assert.False(c.PuedeIntentar(Ahora));
    }

    [Fact]
    public void Un_exito_despues_de_fallos_borra_el_castigo()
    {
        var c = Nueva();
        c.RegistrarFallo("503", Ahora);
        c.RegistrarFallo("503", Ahora);
        Assert.False(c.PuedeIntentar(Ahora));

        c.RegistrarExito();

        Assert.True(c.PuedeIntentar(Ahora));
        Assert.Equal(0, c.Intentos);
    }

    [Fact]
    public void El_error_guardado_se_recorta_para_caber_en_la_columna()
    {
        var c = Nueva();

        c.RegistrarFallo(new string('x', 1000), Ahora);

        Assert.Equal(300, c.UltimoError!.Length);
    }

    [Fact]
    public void Conectar_exige_empleado_y_token()
    {
        Assert.Throws<DomainException>(() => CuentaGoogle.Conectar(0, "a@b.c", "x", Ahora));
        Assert.Throws<DomainException>(() => CuentaGoogle.Conectar(1, "a@b.c", " ", Ahora));
    }
}
