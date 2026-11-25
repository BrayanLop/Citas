using System.Text.RegularExpressions;
using Citas.Application.Agendamiento;
using Citas.Application.Calendario;
using Citas.Domain.Agendamiento;
using Citas.Domain.Calendario;
using Citas.Models;
using Citas.Tests.Soporte;
using Microsoft.Extensions.Logging.Abstractions;

namespace Citas.Tests.Calendario;

/// <summary>El negocio de ejemplo con un barbero (empleado 10) que ya conectó su Google Calendar.</summary>
public sealed class MundoGoogle
{
    public Escenario E { get; } = new Escenario().ComoAdministrador();
    public CalendarioRepositorioMemoria Repo { get; }
    public CalendarioExternoFalso Google { get; } = new();
    public CifradorFalso Cifrador { get; } = new();
    public SincronizadorCalendario Sincronizador { get; }
    public OpcionesGoogle Opciones { get; }

    public MundoGoogle(string? urlWebhook = null)
    {
        Repo = new CalendarioRepositorioMemoria(E.Agenda);
        Opciones = new OpcionesGoogle
        {
            ClientId = "id", ClientSecret = "secreto", RedirectUri = "https://x/Google/Callback",
            ClaveCifradoBase64 = Convert.ToBase64String(new byte[32]), UrlWebhook = urlWebhook,
        };
        Sincronizador = new SincronizadorCalendario(
            Repo, E.Citas, E.Agenda, new UnidadTrabajoMemoria(), Google, Cifrador,
            new FirmasGoogle(Opciones), E.Reloj, Opciones, NullLogger<SincronizadorCalendario>.Instance);
    }

    public async Task ConectarBarberoAsync(int empleado = Escenario.Empleado)
        => await Repo.AgregarCuentaAsync(
            CuentaGoogle.Conectar(empleado, "barbero@gmail.com", Cifrador.Cifrar("refresh-abc"), E.Reloj.AhoraUtc));

    public Task Sync(int empleado = Escenario.Empleado) => Sincronizador.SincronizarEmpleadoAsync("T1", empleado);

    public CuentaGoogle Cuenta(int empleado = Escenario.Empleado) => Repo.Cuentas.Single(c => c.IdEmpleado == empleado);

    public string IdEvento(int idCita) => SincronizadorCalendario.IdEvento("T1", idCita);

    /// <summary>Evento ocupado de Google en una hora local de Bogotá del lunes.</summary>
    public EventoOcupado Ocupado(string id, string desde, string hasta, bool ocupa = true, bool cancelado = false, bool deCitas = false)
        => new(id, E.Utc(Escenario.Lunes, desde), E.Utc(Escenario.Lunes, hasta), false, cancelado, ocupa, deCitas);
}

public class SalidaCitasAGoogleTests
{
    private readonly MundoGoogle _m = new();

    [Fact]
    public async Task Una_cita_nueva_aparece_en_el_calendario_del_barbero()
    {
        await _m.ConectarBarberoAsync();
        var id = await _m.E.AgendarAsync(Escenario.Lunes, "10:00");

        await _m.Sync();

        var (eventoId, evento) = Assert.Single(_m.Google.Publicados);
        Assert.Equal(_m.IdEvento(id), eventoId);
        Assert.Equal(_m.E.Utc(Escenario.Lunes, "10:00"), evento.InicioUtc);
        Assert.Equal(_m.E.Utc(Escenario.Lunes, "10:30"), evento.FinUtc);
        Assert.Contains("Corte", evento.Titulo);
        Assert.Contains("ana", evento.Titulo);
        Assert.StartsWith("[Pendiente]", evento.Titulo);
        Assert.Equal(id, Assert.Single(_m.Repo.VinculosCita).Vinculo.IdCita);
    }

    [Fact]
    public async Task El_refresh_token_se_usa_descifrado_y_nunca_el_cifrado()
    {
        await _m.ConectarBarberoAsync();
        await _m.E.AgendarAsync(Escenario.Lunes, "10:00");

        await _m.Sync();

        Assert.All(_m.Google.TokensUsados, t => Assert.Equal("refresh-abc", t));
        Assert.DoesNotContain("refresh-abc", _m.Cuenta().RefreshTokenCifrado);
    }

    [Fact]
    public async Task Repetir_la_sincronizacion_no_duplica_ni_vuelve_a_publicar()
    {
        await _m.ConectarBarberoAsync();
        await _m.E.AgendarAsync(Escenario.Lunes, "10:00");

        await _m.Sync();
        await _m.Sync();
        await _m.Sync();

        Assert.Equal(1, _m.Google.LlamadasPublicar);
        Assert.Single(_m.Repo.VinculosCita);
    }

    [Fact]
    public async Task El_id_del_evento_es_valido_para_google_y_distinto_por_empresa()
    {
        var a = SincronizadorCalendario.IdEvento("T1", 7);
        var b = SincronizadorCalendario.IdEvento("T2", 7);

        Assert.Matches(new Regex("^[a-v0-9]{5,1024}$"), a);
        Assert.NotEqual(a, b);
        Assert.Equal(a, SincronizadorCalendario.IdEvento("T1", 7));   // determinista
    }

    [Fact]
    public async Task Reprogramar_actualiza_el_mismo_evento_en_vez_de_crear_otro()
    {
        await _m.ConectarBarberoAsync();
        var id = await _m.E.AgendarAsync(Escenario.Lunes, "10:00");
        await _m.Sync();

        await _m.E.CitasApp.ReprogramarAsync(id, new UpdateCitaDto { HoraEstimadaCita = new TimeSpan(11, 0, 0) });
        await _m.Sync();

        var (eventoId, evento) = Assert.Single(_m.Google.Publicados);
        Assert.Equal(_m.IdEvento(id), eventoId);
        Assert.Equal(_m.E.Utc(Escenario.Lunes, "11:00"), evento.InicioUtc);
        Assert.Equal(2, _m.Google.LlamadasPublicar);
    }

    [Fact]
    public async Task Confirmar_la_cita_quita_la_etiqueta_de_pendiente()
    {
        await _m.ConectarBarberoAsync();
        var id = await _m.E.AgendarAsync(Escenario.Lunes, "10:00");
        await _m.Sync();

        await _m.E.CitasApp.CambiarEstadoAsync(id, new PartialUpdateCitaDto { Estado = EstadoCita.Confirmada });
        await _m.Sync();

        Assert.DoesNotContain("[Pendiente]", _m.Google.Publicados[_m.IdEvento(id)].Titulo);
    }

    [Fact]
    public async Task Cancelar_la_cita_borra_el_evento_y_su_vinculo()
    {
        await _m.ConectarBarberoAsync();
        var id = await _m.E.AgendarAsync(Escenario.Lunes, "10:00");
        await _m.Sync();

        await _m.E.CitasApp.CambiarEstadoAsync(id, new PartialUpdateCitaDto { Estado = EstadoCita.Cancelada });
        await _m.Sync();

        Assert.Empty(_m.Google.Publicados);
        Assert.Equal(_m.IdEvento(id), Assert.Single(_m.Google.Eliminados));
        Assert.Empty(_m.Repo.VinculosCita);
    }

    [Fact]
    public async Task Atender_la_cita_no_la_borra_del_calendario()
    {
        await _m.ConectarBarberoAsync();
        var id = await _m.E.AgendarAsync(Escenario.Lunes, "10:00");
        await _m.E.CitasApp.CambiarEstadoAsync(id, new PartialUpdateCitaDto { Estado = EstadoCita.Confirmada });
        await _m.Sync();

        await _m.E.CitasApp.CambiarEstadoAsync(id, new PartialUpdateCitaDto { Estado = EstadoCita.Atendida });
        await _m.Sync();

        Assert.Single(_m.Google.Publicados);
        Assert.Empty(_m.Google.Eliminados);
    }

    [Fact]
    public async Task Eliminar_la_cita_borra_su_evento_aunque_la_cita_ya_no_exista()
    {
        await _m.ConectarBarberoAsync();
        var id = await _m.E.AgendarAsync(Escenario.Lunes, "10:00");
        await _m.Sync();

        await _m.E.CitasApp.EliminarAsync(id);
        await _m.Sync();

        Assert.Empty(_m.Google.Publicados);
        Assert.Empty(_m.Repo.VinculosCita);
    }

    [Fact]
    public async Task Pasar_la_cita_a_otro_barbero_la_retira_del_calendario_del_primero()
    {
        _m.E.Usuarios.Datos.Add(Usuario.Rehidratar(12, "otro", null, true, null, 503));
        await _m.E.Agenda.ReemplazarHorarioAsync(AgendaEmpleado.Definir(12, Escenario.LunesAViernes((new TimeOnly(9, 0), new TimeOnly(18, 0)))));
        await _m.ConectarBarberoAsync();
        var id = await _m.E.AgendarAsync(Escenario.Lunes, "10:00");
        await _m.Sync();

        await _m.E.CitasApp.ReprogramarAsync(id, new UpdateCitaDto { IdEmpleado = 12 });
        await _m.Sync();

        Assert.Empty(_m.Google.Publicados);
        Assert.Equal(_m.IdEvento(id), Assert.Single(_m.Google.Eliminados));
    }

    [Fact]
    public async Task Las_citas_de_otro_barbero_no_aparecen_en_este_calendario()
    {
        _m.E.Usuarios.Datos.Add(Usuario.Rehidratar(12, "otro", null, true, null, 503));
        await _m.E.Agenda.ReemplazarHorarioAsync(AgendaEmpleado.Definir(12, Escenario.LunesAViernes((new TimeOnly(9, 0), new TimeOnly(18, 0)))));
        await _m.ConectarBarberoAsync();
        await _m.E.CitasApp.AgendarAsync(_m.E.Dto(Escenario.Lunes, "10:00", Escenario.Cliente, 12));

        await _m.Sync();

        Assert.Empty(_m.Google.Publicados);
    }

    [Fact]
    public async Task Una_cita_que_ya_terminó_antes_de_conectar_no_se_publica_pero_una_en_curso_si()
    {
        await _m.ConectarBarberoAsync();
        var corte = _m.E.Servicios.Datos[0];
        // Hoy 10:00 local: una terminó a las 09:00, otra va de 09:45 a 10:15.
        await _m.E.Citas.AgregarAsync(Cita.Programar(Escenario.Cliente, Escenario.Empleado, _m.E.Utc(Escenario.Hoy, "08:30"), null, [corte]));
        await _m.E.Citas.AgregarAsync(Cita.Programar(Escenario.OtroCliente, Escenario.Empleado, _m.E.Utc(Escenario.Hoy, "09:45"), null, [corte]));

        await _m.Sync();

        var evento = Assert.Single(_m.Google.Publicados).Value;
        Assert.Equal(_m.E.Utc(Escenario.Hoy, "09:45"), evento.InicioUtc);
    }

    [Fact]
    public async Task Una_cita_que_pasó_y_ya_estaba_publicada_se_queda_en_el_calendario()
    {
        await _m.ConectarBarberoAsync();
        var id = await _m.E.AgendarAsync(Escenario.Lunes, "10:00");
        await _m.Sync();

        _m.E.Tiempo.Avanzar(TimeSpan.FromDays(10));   // la cita ya pasó
        await _m.Sync();

        Assert.Single(_m.Google.Publicados);
        Assert.Empty(_m.Google.Eliminados);
    }

    [Fact]
    public async Task Sin_cuenta_conectada_no_se_llama_a_google()
    {
        await _m.E.AgendarAsync(Escenario.Lunes, "10:00");

        await _m.Sync();

        Assert.Equal(0, _m.Google.LlamadasPublicar);
        Assert.Equal(0, _m.Google.LlamadasListar);
    }
}

public class FallosYRevocacionTests
{
    private readonly MundoGoogle _m = new();

    [Fact]
    public async Task Si_google_revoca_el_permiso_la_cuenta_pasa_a_revocada_y_se_limpian_los_bloqueos_importados()
    {
        await _m.ConectarBarberoAsync();
        // Un bloqueo que vino de Google antes de la revocación.
        var bloqueo = BloqueoAgenda.Crear(Escenario.Empleado, _m.E.Utc(Escenario.Lunes, "10:00"), _m.E.Utc(Escenario.Lunes, "11:00"), "Ocupado");
        await _m.E.Agenda.AgregarBloqueoAsync(bloqueo);
        await _m.Repo.AgregarVinculoBloqueoAsync(Escenario.Empleado, bloqueo.IdBloqueo, "g1");
        await _m.E.AgendarAsync(Escenario.Lunes, "14:00");
        _m.Google.Fallo = new CredencialesRevocadasException("invalid_grant");

        await _m.Sync();   // no debe lanzar

        Assert.Equal(EstadoCuentaGoogle.Revocada, _m.Cuenta().Estado);
        Assert.False(_m.Cuenta().EstaActiva);
        Assert.Empty(_m.E.Agenda.Bloqueos);
        Assert.Contains("invalid_grant", _m.Cuenta().UltimoError);
    }

    [Fact]
    public async Task Una_cuenta_revocada_no_se_vuelve_a_intentar_hasta_reconectar()
    {
        await _m.ConectarBarberoAsync();
        await _m.E.AgendarAsync(Escenario.Lunes, "10:00");
        _m.Google.Fallo = new CredencialesRevocadasException("invalid_grant");
        await _m.Sync();
        _m.Google.Fallo = null;

        await _m.Sync();

        Assert.Equal(0, _m.Google.LlamadasPublicar);
    }

    [Fact]
    public async Task La_agenda_de_citas_sigue_funcionando_con_la_cuenta_revocada()
    {
        await _m.ConectarBarberoAsync();
        _m.Google.Fallo = new CredencialesRevocadasException("invalid_grant");
        await _m.Sync();

        var id = await _m.E.AgendarAsync(Escenario.Lunes, "10:00");

        Assert.True(id > 0);
    }

    [Fact]
    public async Task Un_fallo_transitorio_reintenta_con_espera_y_se_recupera_solo()
    {
        await _m.ConectarBarberoAsync();
        var id = await _m.E.AgendarAsync(Escenario.Lunes, "10:00");
        _m.Google.Fallo = new HttpRequestException("Google Calendar respondió 503");

        await _m.Sync();

        Assert.Equal(1, _m.Cuenta().Intentos);
        Assert.Equal(_m.E.Reloj.AhoraUtc.AddMinutes(1), _m.Cuenta().ProximoIntentoUtc);
        Assert.True(_m.Cuenta().EstaActiva);          // un fallo transitorio no revoca nada

        _m.Google.Fallo = null;
        await _m.Sync();                              // todavía en espera: no llama
        Assert.Empty(_m.Google.Publicados);

        _m.E.Tiempo.Avanzar(TimeSpan.FromMinutes(2));
        await _m.Sync();                              // pasó la espera: publica

        Assert.Equal(_m.IdEvento(id), Assert.Single(_m.Google.Publicados).Key);
        Assert.Equal(0, _m.Cuenta().Intentos);
        Assert.Null(_m.Cuenta().UltimoError);
    }

    [Fact]
    public async Task La_espera_entre_reintentos_crece_y_tiene_tope()
    {
        await _m.ConectarBarberoAsync();
        await _m.E.AgendarAsync(Escenario.Lunes, "10:00");
        _m.Google.Fallo = new HttpRequestException("caído");
        var esperas = new List<double>();

        for (var i = 0; i < 9; i++)
        {
            var antes = _m.E.Reloj.AhoraUtc;
            await _m.Sync();
            esperas.Add((_m.Cuenta().ProximoIntentoUtc!.Value - antes).TotalMinutes);
            _m.E.Tiempo.Avanzar(TimeSpan.FromMinutes(61));
        }

        Assert.Equal([1, 2, 4, 8, 16, 32, 60, 60, 60], esperas);
    }

    [Fact]
    public async Task Un_fallo_a_mitad_de_la_salida_no_duplica_lo_que_si_se_publicó()
    {
        await _m.ConectarBarberoAsync();
        await _m.E.AgendarAsync(Escenario.Lunes, "10:00");
        await _m.E.AgendarAsync(Escenario.Lunes, "11:00", Escenario.OtroCliente);
        await _m.Sync();
        Assert.Equal(2, _m.Google.Publicados.Count);

        // Se agrega una tercera y Google falla: las dos primeras no se tocan.
        await _m.E.AgendarAsync(Escenario.Lunes, "15:00");
        _m.Google.Fallo = new HttpRequestException("caído");
        await _m.Sync();
        _m.Google.Fallo = null;
        _m.E.Tiempo.Avanzar(TimeSpan.FromMinutes(5));
        await _m.Sync();

        Assert.Equal(3, _m.Google.Publicados.Count);
        Assert.Equal(3, _m.Repo.VinculosCita.Count);
        Assert.Equal(3, _m.Google.LlamadasPublicar);   // 2 + 1: ninguna se publicó dos veces
    }
}

public class EntradaGoogleABloqueosTests
{
    private readonly MundoGoogle _m = new();

    private CambiosCalendario Completo(params EventoOcupado[] eventos) => new(eventos, "sync-1", true);

    private async Task<List<string>> HorasLibresLunes()
    {
        var r = await _m.E.Disponibilidad.HorasLibresAsync(Escenario.Empleado, Escenario.Lunes, [1]);
        return r.Horas.Select(h => h.ToString(@"hh\:mm")).ToList();
    }

    [Fact]
    public async Task Un_evento_personal_del_barbero_bloquea_esa_hora_para_los_clientes()
    {
        await _m.ConectarBarberoAsync();
        Assert.Contains("10:00", await HorasLibresLunes());
        _m.Google.Cambios = _ => Completo(_m.Ocupado("g1", "10:00", "11:00"));

        await _m.Sync();

        var horas = await HorasLibresLunes();
        Assert.DoesNotContain("10:00", horas);
        Assert.DoesNotContain("09:45", horas);   // un corte de 30 min desde las 09:45 pisaría el evento
        Assert.Contains("09:30", horas);
        Assert.Contains("11:00", horas);
    }

    [Fact]
    public async Task El_bloqueo_no_copia_el_titulo_del_evento_que_es_privado()
    {
        await _m.ConectarBarberoAsync();
        _m.Google.Cambios = _ => Completo(_m.Ocupado("g1", "10:00", "11:00"));

        await _m.Sync();

        var bloqueo = Assert.Single(_m.E.Agenda.Bloqueos);
        Assert.Equal("Ocupado en Google Calendar", bloqueo.Motivo);
        Assert.Equal(_m.E.Utc(Escenario.Lunes, "10:00"), bloqueo.Inicio);
        Assert.Equal("g1", Assert.Single(_m.Repo.VinculosBloqueo).Vinculo.GoogleEventId);
    }

    [Fact]
    public async Task Un_evento_cancelado_en_google_libera_la_hora()
    {
        await _m.ConectarBarberoAsync();
        _m.Google.Cambios = tok => tok is null
            ? Completo(_m.Ocupado("g1", "10:00", "11:00"))
            : new CambiosCalendario([_m.Ocupado("g1", "10:00", "11:00", cancelado: true)], "sync-2", false);
        await _m.Sync();
        Assert.Single(_m.E.Agenda.Bloqueos);

        _m.E.Tiempo.Avanzar(TimeSpan.FromMinutes(6));
        await _m.Sync();

        Assert.Empty(_m.E.Agenda.Bloqueos);
        Assert.Empty(_m.Repo.VinculosBloqueo);
        Assert.Contains("10:00", await HorasLibresLunes());
    }

    [Fact]
    public async Task Un_evento_que_cambia_de_hora_mueve_el_bloqueo_sin_dejar_el_anterior()
    {
        await _m.ConectarBarberoAsync();
        _m.Google.Cambios = tok => tok is null
            ? Completo(_m.Ocupado("g1", "10:00", "11:00"))
            : new CambiosCalendario([_m.Ocupado("g1", "15:00", "16:00")], "sync-2", false);
        await _m.Sync();

        _m.E.Tiempo.Avanzar(TimeSpan.FromMinutes(6));
        await _m.Sync();

        var bloqueo = Assert.Single(_m.E.Agenda.Bloqueos);
        Assert.Equal(_m.E.Utc(Escenario.Lunes, "15:00"), bloqueo.Inicio);
    }

    [Fact]
    public async Task Los_eventos_libres_los_creados_por_citas_y_los_ya_pasados_no_bloquean()
    {
        await _m.ConectarBarberoAsync();
        var ayer = new EventoOcupado("g-pasado", _m.E.Utc(Escenario.Hoy, "07:00"), _m.E.Utc(Escenario.Hoy, "08:00"), false, false, true, false);
        _m.Google.Cambios = _ => Completo(
            _m.Ocupado("g-libre", "10:00", "11:00", ocupa: false),                 // marcado como "disponible"
            _m.Ocupado("g-nuestro", "11:00", "12:00", deCitas: true),              // lo publicó Citas
            _m.Ocupado(SincronizadorCalendario.IdEvento("T1", 5), "14:00", "15:00"), // id de Citas
            ayer);

        await _m.Sync();

        Assert.Empty(_m.E.Agenda.Bloqueos);
    }

    [Fact]
    public async Task Un_evento_de_dia_completo_bloquea_el_dia_local_entero()
    {
        await _m.ConectarBarberoAsync();
        _m.Google.Cambios = _ => Completo(new EventoOcupado(
            "g-dia", new DateTime(2026, 11, 30), new DateTime(2026, 12, 1), true, false, true, false));

        await _m.Sync();

        var bloqueo = Assert.Single(_m.E.Agenda.Bloqueos);
        Assert.Equal(_m.E.Utc(Escenario.Lunes, "00:00"), bloqueo.Inicio);                       // medianoche de Bogotá
        Assert.Equal(_m.E.Utc(Escenario.Lunes.AddDays(1), "00:00"), bloqueo.Fin);
        Assert.Empty(await HorasLibresLunes());
    }

    [Fact]
    public async Task Con_el_token_vencido_se_hace_una_carga_completa_sin_duplicar_bloqueos()
    {
        await _m.ConectarBarberoAsync();
        var vencido = false;
        _m.Google.Cambios = tok =>
        {
            if (tok == "sync-1" && !vencido) { vencido = true; throw new SyncTokenVencidoException(); }
            return new CambiosCalendario([_m.Ocupado("g1", "10:00", "11:00")], tok is null ? "sync-1" : "sync-2", true);
        };
        await _m.Sync();

        _m.E.Tiempo.Avanzar(TimeSpan.FromMinutes(6));
        await _m.Sync();

        Assert.Single(_m.E.Agenda.Bloqueos);
        Assert.Equal([null, "sync-1", null], _m.Google.SyncTokensPedidos);
        Assert.Equal("sync-1", _m.Cuenta().SyncToken);
    }

    [Fact]
    public async Task La_lectura_de_google_respeta_el_intervalo_pero_un_aviso_la_adelanta()
    {
        await _m.ConectarBarberoAsync();
        await _m.Sync();
        Assert.Equal(1, _m.Google.LlamadasListar);

        await _m.Sync();                                  // aún no toca
        Assert.Equal(1, _m.Google.LlamadasListar);

        await _m.Sincronizador.MarcarCambioRemotoAsync(Escenario.Empleado);   // webhook de Google
        await _m.Sync();
        Assert.Equal(2, _m.Google.LlamadasListar);

        _m.E.Tiempo.Avanzar(TimeSpan.FromMinutes(6));     // pasó el intervalo
        await _m.Sync();
        Assert.Equal(3, _m.Google.LlamadasListar);
    }

    [Fact]
    public async Task Con_webhook_configurado_se_registra_el_canal_una_sola_vez()
    {
        var m = new MundoGoogle(urlWebhook: "https://citas.example/Google/Notificaciones");
        await m.ConectarBarberoAsync();

        await m.Sync();
        m.E.Tiempo.Avanzar(TimeSpan.FromMinutes(6));
        await m.Sync();

        Assert.Equal(1, m.Google.CanalesRegistrados);
        Assert.NotNull(m.Cuenta().CanalId);
    }

    [Fact]
    public async Task Sin_webhook_configurado_no_se_pide_canal()
    {
        await _m.ConectarBarberoAsync();

        await _m.Sync();

        Assert.Equal(0, _m.Google.CanalesRegistrados);
        Assert.Null(_m.Cuenta().CanalId);
    }
}
