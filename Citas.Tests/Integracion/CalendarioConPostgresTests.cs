using Citas.Application.Agendamiento;
using Citas.Application.Calendario;
using Citas.Domain.Agendamiento;
using Citas.Domain.Calendario;
using Citas.Domain.Common;
using Citas.Models;
using Citas.Tests.Soporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Citas.Tests.Integracion;

/// <summary>
/// La integración con Google Calendar contra PostgreSQL real: el esquema de la migración, el
/// aislamiento entre empresas (incluidos los dos puntos que lo cruzan a propósito) y la
/// sincronización completa con los repositorios de verdad (Google es el único de mentira).
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public class CalendarioConPostgresTests(PostgresFixture bd)
{
    private static async Task<T> En<T>(Negocio n, Func<IServiceProvider, Task<T>> accion)
    {
        using var peticion = n.Peticion();
        return await accion(peticion.ServiceProvider);
    }

    private static Task En(Negocio n, Func<IServiceProvider, Task> accion)
        => En<int>(n, async sp => { await accion(sp); return 0; });

    private static async Task ConectarAsync(Negocio n, string email = "barbero@gmail.com")
        => await En(n, async sp =>
        {
            await sp.GetRequiredService<ICalendarioRepositorio>().AgregarCuentaAsync(
                CuentaGoogle.Conectar(n.Empleado, email, new CifradorFalso().Cifrar("refresh-abc"), n.Reloj.AhoraUtc));
            await sp.GetRequiredService<Citas.Application.Common.IUnitOfWork>().GuardarCambiosAsync();
        });

    private static Task SincronizarAsync(Negocio n)
        => En(n, sp => sp.GetRequiredService<SincronizadorCalendario>().SincronizarEmpleadoAsync(n.Tenant, n.Empleado));

    private static Task<CitaResponseDto> AgendarAsync(Negocio n, string hora, int cliente)
        => En(n, sp => sp.GetRequiredService<CitaAppService>().AgendarAsync(n.Dto(hora, cliente)));

    private static Task<CuentaGoogle?> CuentaAsync(Negocio n)
        => En(n, sp => sp.GetRequiredService<ICalendarioRepositorio>().ObtenerCuentaAsync(n.Empleado));

    [Fact]
    public async Task La_migracion_crea_las_tablas_de_la_integracion()
    {
        await using var db = bd.CrearContexto("verificacion");
        var tablas = await db.Database
            .SqlQuery<string>($"SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_name IN ('CuentasGoogle','GoogleEventosCita','GoogleBloqueos') ORDER BY 1")
            .ToListAsync();

        Assert.Equal(["CuentasGoogle", "GoogleBloqueos", "GoogleEventosCita"], tablas);
    }

    [Fact]
    public async Task La_cuenta_se_guarda_y_se_lee_con_todo_su_estado()
    {
        var n = await bd.CrearNegocioAsync(clientes: 1);
        await ConectarAsync(n);

        var cuenta = (await CuentaAsync(n))!;
        cuenta.RegistrarSincronizacionEntrante("tok-1", n.Reloj.AhoraUtc);
        cuenta.RegistrarCanal("canal-1", n.Reloj.AhoraUtc.AddDays(7));
        cuenta.RegistrarFallo("503", n.Reloj.AhoraUtc);
        await En(n, async sp =>
        {
            sp.GetRequiredService<ICalendarioRepositorio>().ActualizarCuenta(cuenta);
            await sp.GetRequiredService<Citas.Application.Common.IUnitOfWork>().GuardarCambiosAsync();
        });

        var leida = (await CuentaAsync(n))!;
        Assert.Equal("tok-1", leida.SyncToken);
        Assert.Equal("canal-1", leida.CanalId);
        Assert.Equal(1, leida.Intentos);
        Assert.Equal("503", leida.UltimoError);
        Assert.Equal(n.Reloj.AhoraUtc.AddMinutes(1), leida.ProximoIntentoUtc);
        Assert.Equal(DateTimeKind.Utc, leida.ProximoIntentoUtc!.Value.Kind);
        Assert.Equal("enc:" + new string("refresh-abc".Reverse().ToArray()), leida.RefreshTokenCifrado);
    }

    [Fact]
    public async Task Un_empleado_solo_puede_tener_una_cuenta_por_empresa()
    {
        var n = await bd.CrearNegocioAsync(clientes: 1);
        await ConectarAsync(n);

        await Assert.ThrowsAsync<DbUpdateException>(() => ConectarAsync(n, "otra@gmail.com"));
    }

    [Fact]
    public async Task Una_empresa_no_ve_las_cuentas_de_otra()
    {
        var a = await bd.CrearNegocioAsync(clientes: 1);
        var b = await bd.CrearNegocioAsync(clientes: 1);
        await ConectarAsync(a);

        Assert.NotNull(await CuentaAsync(a));
        Assert.Null(await CuentaAsync(b));
    }

    [Fact]
    public async Task El_proceso_de_fondo_ve_las_cuentas_activas_de_todas_las_empresas_y_no_las_revocadas()
    {
        var a = await bd.CrearNegocioAsync(clientes: 1);
        var b = await bd.CrearNegocioAsync(clientes: 1);
        var c = await bd.CrearNegocioAsync(clientes: 1);
        await ConectarAsync(a);
        await ConectarAsync(b);
        await ConectarAsync(c);
        var revocada = (await CuentaAsync(c))!;
        revocada.Revocar("invalid_grant");
        await En(c, async sp =>
        {
            sp.GetRequiredService<ICalendarioRepositorio>().ActualizarCuenta(revocada);
            await sp.GetRequiredService<Citas.Application.Common.IUnitOfWork>().GuardarCambiosAsync();
        });

        var cuentas = await En(a, sp => sp.GetRequiredService<ICalendarioRepositorio>().ListarCuentasActivasAsync());

        Assert.Contains(new CuentaPendiente(a.Tenant, a.Empleado), cuentas);
        Assert.Contains(new CuentaPendiente(b.Tenant, b.Empleado), cuentas);
        Assert.DoesNotContain(cuentas, x => x.Tenant == c.Tenant);
    }

    [Fact]
    public async Task El_webhook_encuentra_la_empresa_por_el_canal()
    {
        var a = await bd.CrearNegocioAsync(clientes: 1);
        await ConectarAsync(a);
        var canal = "canal-" + Guid.NewGuid().ToString("N");
        var cuenta = (await CuentaAsync(a))!;
        cuenta.RegistrarCanal(canal, a.Reloj.AhoraUtc.AddDays(7));
        await En(a, async sp =>
        {
            sp.GetRequiredService<ICalendarioRepositorio>().ActualizarCuenta(cuenta);
            await sp.GetRequiredService<Citas.Application.Common.IUnitOfWork>().GuardarCambiosAsync();
        });

        // Desde otro negocio (el webhook no trae tenant): igual lo encuentra.
        var b = await bd.CrearNegocioAsync(clientes: 1);
        var encontrada = await En(b, sp => sp.GetRequiredService<ICalendarioRepositorio>().BuscarPorCanalAsync(canal));

        Assert.Equal(new CuentaPendiente(a.Tenant, a.Empleado), encontrada);
        Assert.Null(await En(b, sp => sp.GetRequiredService<ICalendarioRepositorio>().BuscarPorCanalAsync("no-existe")));
    }

    // ---- De punta a punta ----

    [Fact]
    public async Task Una_cita_real_llega_al_calendario_y_sigue_sus_cambios()
    {
        var n = await bd.CrearNegocioAsync(clientes: 2);
        await ConectarAsync(n);
        var cita = await AgendarAsync(n, "10:00", n.Clientes[0]);

        await SincronizarAsync(n);

        var (id, evento) = Assert.Single(n.Google.Publicados);
        Assert.Equal(SincronizadorCalendario.IdEvento(n.Tenant, cita.IdCita), id);
        Assert.Equal(n.Reloj.AUtc(Negocio.Lunes, new TimeOnly(10, 0)), evento.InicioUtc);
        Assert.Contains("Corte", evento.Titulo);
        Assert.Contains("cliente-0", evento.Titulo);

        // Repetir no vuelve a publicar.
        await SincronizarAsync(n);
        Assert.Equal(1, n.Google.LlamadasPublicar);

        // Reprogramar actualiza el mismo evento.
        await En(n, sp => sp.GetRequiredService<CitaAppService>().ReprogramarAsync(
            cita.IdCita, new UpdateCitaDto { HoraEstimadaCita = new TimeSpan(11, 0, 0) }));
        await SincronizarAsync(n);
        Assert.Equal(n.Reloj.AUtc(Negocio.Lunes, new TimeOnly(11, 0)), n.Google.Publicados[id].InicioUtc);
        Assert.Equal(2, n.Google.LlamadasPublicar);

        // Cancelar lo borra.
        await En(n, sp => sp.GetRequiredService<CitaAppService>().CambiarEstadoAsync(
            cita.IdCita, new PartialUpdateCitaDto { Estado = EstadoCita.Cancelada }));
        await SincronizarAsync(n);
        Assert.Empty(n.Google.Publicados);
        Assert.Equal(id, Assert.Single(n.Google.Eliminados));
    }

    [Fact]
    public async Task Si_la_cita_se_borra_de_la_base_el_evento_huerfano_se_elimina_de_google()
    {
        var n = await bd.CrearNegocioAsync(clientes: 1);
        await ConectarAsync(n);
        var cita = await AgendarAsync(n, "10:00", n.Clientes[0]);
        await SincronizarAsync(n);
        Assert.Single(n.Google.Publicados);

        await En(n, sp => sp.GetRequiredService<CitaAppService>().EliminarAsync(cita.IdCita));
        await SincronizarAsync(n);

        Assert.Empty(n.Google.Publicados);
        var vinculos = await En(n, sp => sp.GetRequiredService<ICalendarioRepositorio>().ListarVinculosCitaAsync(n.Empleado));
        Assert.Empty(vinculos);
    }

    [Fact]
    public async Task Un_evento_personal_del_barbero_se_vuelve_bloqueo_y_quita_esa_hora_de_la_disponibilidad()
    {
        var n = await bd.CrearNegocioAsync(clientes: 1);
        await ConectarAsync(n);
        n.Google.Cambios = _ => new CambiosCalendario(
            [new EventoOcupado("g1", n.Reloj.AUtc(Negocio.Lunes, new TimeOnly(10, 0)), n.Reloj.AUtc(Negocio.Lunes, new TimeOnly(11, 0)), false, false, true, false)],
            "sync-1", true);

        await SincronizarAsync(n);

        var libres = await En(n, sp => sp.GetRequiredService<DisponibilidadAppService>().HorasLibresAsync(n.Empleado, Negocio.Lunes, [n.ServicioCorte]));
        var horas = libres.Horas.Select(h => h.ToString(@"hh\:mm")).ToList();
        Assert.DoesNotContain("10:00", horas);
        Assert.Contains("09:30", horas);
        Assert.Contains("11:00", horas);

        // Y quien intente agendar en ese hueco recibe el rechazo normal.
        await Assert.ThrowsAsync<ConflictException>(() => AgendarAsync(n, "10:15", n.Clientes[0]));
    }

    [Fact]
    public async Task Borrar_el_evento_en_google_libera_la_hora_y_no_deja_vinculos_sueltos()
    {
        var n = await bd.CrearNegocioAsync(clientes: 1);
        await ConectarAsync(n);
        EventoOcupado Evento(bool cancelado) => new("g1", n.Reloj.AUtc(Negocio.Lunes, new TimeOnly(10, 0)), n.Reloj.AUtc(Negocio.Lunes, new TimeOnly(11, 0)), false, cancelado, true, false);
        n.Google.Cambios = tok => tok is null ? new CambiosCalendario([Evento(false)], "sync-1", true) : new CambiosCalendario([Evento(true)], "sync-2", false);
        await SincronizarAsync(n);

        n.Tiempo.Avanzar(TimeSpan.FromMinutes(6));
        await SincronizarAsync(n);

        var bloqueos = await En(n, sp => sp.GetRequiredService<IAgendaEmpleadoRepository>()
            .ListarBloqueosAsync(n.Empleado, n.Reloj.AUtc(Negocio.Lunes, new TimeOnly(0, 0)), n.Reloj.AUtc(Negocio.Lunes.AddDays(1), new TimeOnly(0, 0))));
        Assert.Empty(bloqueos);
        Assert.Empty(await En(n, sp => sp.GetRequiredService<ICalendarioRepositorio>().ListarVinculosBloqueoAsync(n.Empleado)));
    }

    [Fact]
    public async Task Al_revocar_google_se_borran_los_bloqueos_importados_pero_no_los_que_puso_el_negocio()
    {
        var n = await bd.CrearNegocioAsync(clientes: 1);
        await ConectarAsync(n);
        n.Google.Cambios = _ => new CambiosCalendario(
            [new EventoOcupado("g1", n.Reloj.AUtc(Negocio.Lunes, new TimeOnly(10, 0)), n.Reloj.AUtc(Negocio.Lunes, new TimeOnly(11, 0)), false, false, true, false)],
            "sync-1", true);
        await SincronizarAsync(n);
        // Un bloqueo manual del negocio (vacaciones) que NO viene de Google.
        await En(n, async sp =>
        {
            await sp.GetRequiredService<IAgendaEmpleadoRepository>().AgregarBloqueoAsync(
                BloqueoAgenda.Crear(n.Empleado, n.Reloj.AUtc(Negocio.Lunes.AddDays(1), new TimeOnly(9, 0)), n.Reloj.AUtc(Negocio.Lunes.AddDays(1), new TimeOnly(18, 0)), "Vacaciones"));
            await sp.GetRequiredService<Citas.Application.Common.IUnitOfWork>().GuardarCambiosAsync();
        });

        n.Google.Fallo = new CredencialesRevocadasException("invalid_grant");
        n.Tiempo.Avanzar(TimeSpan.FromMinutes(6));
        await SincronizarAsync(n);

        Assert.Equal(EstadoCuentaGoogle.Revocada, (await CuentaAsync(n))!.Estado);
        var bloqueos = await En(n, sp => sp.GetRequiredService<IAgendaEmpleadoRepository>()
            .ListarBloqueosAsync(n.Empleado, n.Reloj.AUtc(Negocio.Lunes, new TimeOnly(0, 0)), n.Reloj.AUtc(Negocio.Lunes.AddDays(3), new TimeOnly(0, 0))));
        Assert.Equal("Vacaciones", Assert.Single(bloqueos).Motivo);
    }

    [Fact]
    public async Task Borrar_los_bloqueos_de_google_de_una_empresa_no_toca_los_de_otra_con_el_mismo_evento()
    {
        var a = await bd.CrearNegocioAsync(clientes: 1);
        var b = await bd.CrearNegocioAsync(clientes: 1);
        foreach (var n in new[] { a, b })
        {
            await ConectarAsync(n);
            n.Google.Cambios = _ => new CambiosCalendario(
                [new EventoOcupado("mismo-evento", n.Reloj.AUtc(Negocio.Lunes, new TimeOnly(10, 0)), n.Reloj.AUtc(Negocio.Lunes, new TimeOnly(11, 0)), false, false, true, false)],
                "sync-1", true);
            await SincronizarAsync(n);
        }

        await En(a, sp => sp.GetRequiredService<ICalendarioRepositorio>().EliminarBloqueosDeGoogleAsync(a.Empleado, null));

        Assert.Empty(await En(a, sp => sp.GetRequiredService<ICalendarioRepositorio>().ListarVinculosBloqueoAsync(a.Empleado)));
        Assert.Single(await En(b, sp => sp.GetRequiredService<ICalendarioRepositorio>().ListarVinculosBloqueoAsync(b.Empleado)));
    }

    [Fact]
    public async Task Eliminar_la_cuenta_borra_tambien_los_vinculos_de_citas()
    {
        var n = await bd.CrearNegocioAsync(clientes: 1);
        await ConectarAsync(n);
        await AgendarAsync(n, "10:00", n.Clientes[0]);
        await SincronizarAsync(n);
        Assert.Single(await En(n, sp => sp.GetRequiredService<ICalendarioRepositorio>().ListarVinculosCitaAsync(n.Empleado)));

        await En(n, sp => sp.GetRequiredService<ICalendarioRepositorio>().EliminarCuentaAsync(n.Empleado));

        Assert.Null(await CuentaAsync(n));
        Assert.Empty(await En(n, sp => sp.GetRequiredService<ICalendarioRepositorio>().ListarVinculosCitaAsync(n.Empleado)));
    }
}
