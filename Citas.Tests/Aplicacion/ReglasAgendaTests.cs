using Citas.Domain.Common;
using Citas.Models;
using Citas.Tests.Soporte;

namespace Citas.Tests.Aplicacion;

/// <summary>Validación de un hueco concreto: lo que decide si una cita puede crearse.</summary>
public class ValidarHuecoTests
{
    private readonly Escenario _e = new();

    private Task Validar(DateOnly fecha, string inicio, int minutos, int? excluir = null)
    {
        var ini = _e.Utc(fecha, inicio);
        return _e.Reglas.ValidarHuecoAsync(Escenario.Empleado, ini, ini.AddMinutes(minutos), excluir, default);
    }

    [Fact]
    public Task Un_hueco_dentro_del_horario_es_valido() => Validar(Escenario.Lunes, "10:00", 30);

    [Fact]
    public Task Un_hueco_que_termina_justo_al_cierre_de_la_franja_es_valido() => Validar(Escenario.Lunes, "11:30", 30);

    [Fact]
    public async Task Antes_de_abrir_es_conflicto()
        => await Assert.ThrowsAsync<ConflictException>(() => Validar(Escenario.Lunes, "08:30", 30));

    [Fact]
    public async Task Pasarse_del_cierre_de_la_franja_es_conflicto()
        => await Assert.ThrowsAsync<ConflictException>(() => Validar(Escenario.Lunes, "11:45", 30));

    [Fact]
    public async Task Cruzar_el_almuerzo_es_conflicto()
        => await Assert.ThrowsAsync<ConflictException>(() => Validar(Escenario.Lunes, "11:30", 180));

    [Fact]
    public async Task El_dia_que_no_trabaja_es_conflicto_y_lo_dice()
    {
        var error = await Assert.ThrowsAsync<ConflictException>(() => Validar(Escenario.Domingo, "10:00", 30));
        Assert.Contains("no trabaja", error.Message);
    }

    [Fact]
    public async Task Una_cita_en_el_pasado_es_error_de_validacion()
        => await Assert.ThrowsAsync<DomainException>(() => Validar(Escenario.Hoy, "09:00", 30));

    [Fact]
    public async Task Respeta_la_anticipacion_minima()
    {
        var e = new Escenario(anticipacionMinutos: 120);
        var ini = e.Utc(Escenario.Hoy, "11:00");   // ahora 10:00 + 2 h = 12:00
        await Assert.ThrowsAsync<DomainException>(() =>
            e.Reglas.ValidarHuecoAsync(Escenario.Empleado, ini, ini.AddMinutes(30), null, default));
    }

    [Fact]
    public async Task Un_bloqueo_impide_la_cita_y_dice_el_motivo()
    {
        await _e.Agenda.AgregarBloqueoAsync(BloqueoAgenda.Crear(
            Escenario.Empleado, _e.Utc(Escenario.Lunes, "10:00"), _e.Utc(Escenario.Lunes, "11:00"), "Cita médica"));

        var error = await Assert.ThrowsAsync<ConflictException>(() => Validar(Escenario.Lunes, "10:30", 30));
        Assert.Contains("Cita médica", error.Message);
    }

    [Fact]
    public async Task Otra_cita_activa_impide_el_cruce()
    {
        await _e.AgendarAsync(Escenario.Lunes, "10:00");
        await Assert.ThrowsAsync<ConflictException>(() => Validar(Escenario.Lunes, "10:15", 30));
    }

    [Fact]
    public async Task Citas_contiguas_si_se_permiten()
    {
        await _e.AgendarAsync(Escenario.Lunes, "10:00");   // 10:00-10:30
        await Validar(Escenario.Lunes, "10:30", 30);       // justo después
        await Validar(Escenario.Lunes, "09:30", 30);       // justo antes
    }

    [Fact]
    public async Task Una_cita_cancelada_ya_no_ocupa_la_agenda()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00");
        (await _e.Citas.ObtenerPorIdAsync(id))!.Cancelar();

        await Validar(Escenario.Lunes, "10:00", 30);
    }

    [Fact]
    public async Task Una_cita_atendida_tampoco_ocupa_la_agenda()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00");
        var cita = (await _e.Citas.ObtenerPorIdAsync(id))!;
        cita.Confirmar();
        cita.Atender();

        await Validar(Escenario.Lunes, "10:00", 30);
    }

    [Fact]
    public async Task Al_reprogramar_la_propia_cita_no_cuenta_como_cruce()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00");
        await Validar(Escenario.Lunes, "10:00", 30, excluir: id);
    }

    [Fact]
    public async Task Otro_empleado_no_ve_las_citas_de_este()
    {
        await _e.AgendarAsync(Escenario.Lunes, "10:00");
        // El empleado 11 no tiene horario: el error es de horario, no de cruce.
        var ini = _e.Utc(Escenario.Lunes, "10:00");
        await Assert.ThrowsAsync<ConflictException>(() =>
            _e.Reglas.ValidarHuecoAsync(Escenario.EmpleadoInactivo, ini, ini.AddMinutes(30), null, default));
    }
}

/// <summary>Horas libres y días con hueco que se le ofrecen al cliente.</summary>
public class DisponibilidadTests
{
    private readonly Escenario _e = new();

    private async Task<List<string>> Horas(DateOnly fecha, params int[] servicios)
    {
        var r = await _e.Disponibilidad.HorasLibresAsync(Escenario.Empleado, fecha, servicios.Length == 0 ? [1] : servicios);
        return r.Horas.Select(h => h.ToString(@"hh\:mm")).ToList();
    }

    [Fact]
    public async Task Sin_citas_ofrece_todo_el_horario_cada_quince_minutos()
    {
        var horas = await Horas(Escenario.Lunes);

        Assert.Equal("09:00", horas.First());
        Assert.Contains("11:30", horas);        // ultima de la mañana para 30 min
        Assert.DoesNotContain("11:45", horas);  // se pasaría del cierre
        Assert.DoesNotContain("12:00", horas);  // almuerzo
        Assert.Contains("14:00", horas);
        Assert.Equal("17:30", horas.Last());
        Assert.Equal(11 + 15, horas.Count);     // 09:00-11:30 y 14:00-17:30
    }

    [Fact]
    public async Task Una_cita_quita_las_horas_que_se_le_cruzan_y_deja_las_contiguas()
    {
        await _e.AgendarAsync(Escenario.Lunes, "10:00");   // 10:00-10:30

        var horas = await Horas(Escenario.Lunes);

        Assert.Contains("09:30", horas);        // termina justo cuando empieza la cita
        Assert.DoesNotContain("09:45", horas);  // 09:45-10:15 se cruza
        Assert.DoesNotContain("10:00", horas);
        Assert.DoesNotContain("10:15", horas);
        Assert.Contains("10:30", horas);        // empieza justo cuando termina
    }

    [Fact]
    public async Task Un_servicio_largo_reduce_las_horas_de_inicio_posibles()
    {
        var horas = await Horas(Escenario.Lunes, 3);   // Color: 90 min

        Assert.Contains("10:30", horas);        // 10:30-12:00
        Assert.DoesNotContain("10:45", horas);
    }

    [Fact]
    public async Task Varios_servicios_suman_su_duracion()
    {
        var r = await _e.Disponibilidad.HorasLibresAsync(Escenario.Empleado, Escenario.Lunes, [1, 2]);

        Assert.Equal(45, r.DuracionTotal);
        Assert.Contains(new TimeSpan(11, 15, 0), r.Horas);
        Assert.DoesNotContain(new TimeSpan(11, 30, 0), r.Horas);
    }

    [Fact]
    public async Task Hoy_no_ofrece_horas_que_ya_pasaron()
    {
        var horas = await Horas(Escenario.Hoy);   // ahora son las 10:00

        Assert.DoesNotContain("09:45", horas);
        Assert.Contains("10:00", horas);
        Assert.Contains("14:00", horas);
    }

    [Fact]
    public async Task Un_dia_pasado_no_tiene_horas()
        => Assert.Empty(await Horas(new DateOnly(2026, 11, 24)));

    [Fact]
    public async Task El_dia_que_no_trabaja_no_tiene_horas()
        => Assert.Empty(await Horas(Escenario.Domingo));

    [Fact]
    public async Task Un_bloqueo_quita_esas_horas()
    {
        await _e.Agenda.AgregarBloqueoAsync(BloqueoAgenda.Crear(
            Escenario.Empleado, _e.Utc(Escenario.Lunes, "14:00"), _e.Utc(Escenario.Lunes, "16:00"), "Vacaciones"));

        var horas = await Horas(Escenario.Lunes);

        Assert.DoesNotContain("14:00", horas);
        Assert.DoesNotContain("15:45", horas);
        Assert.Contains("16:00", horas);
    }

    [Fact]
    public async Task El_intervalo_configurado_cambia_las_horas_de_inicio()
    {
        var e = new Escenario(intervaloMinutos: 30);
        var r = await e.Disponibilidad.HorasLibresAsync(Escenario.Empleado, Escenario.Lunes, [1]);

        Assert.All(r.Horas, h => Assert.True(h.Minutes is 0 or 30));
        Assert.Equal(30, r.IntervaloMinutos);
    }

    [Fact]
    public async Task Las_citas_del_dia_anterior_no_afectan_a_este_dia()
    {
        await _e.AgendarAsync(new DateOnly(2026, 12, 1), "10:00");
        Assert.Contains("10:00", await Horas(Escenario.Lunes));
    }

    [Fact]
    public async Task Dias_con_hueco_excluye_los_dias_que_no_trabaja()
    {
        var r = await _e.Disponibilidad.DiasConHuecoAsync(
            Escenario.Empleado, Escenario.Lunes, new DateOnly(2026, 12, 6), [1]);

        Assert.Equal(5, r.Dias.Count);   // lunes a viernes
        Assert.DoesNotContain(new DateOnly(2026, 12, 5), r.Dias);   // sábado
        Assert.DoesNotContain(new DateOnly(2026, 12, 6), r.Dias);   // domingo
    }

    [Fact]
    public async Task Un_dia_completamente_ocupado_deja_de_aparecer()
    {
        // 09:00-12:00 y 14:00-18:00 llenos con bloqueos.
        await _e.Agenda.AgregarBloqueoAsync(BloqueoAgenda.Crear(
            Escenario.Empleado, _e.Utc(Escenario.Lunes, "09:00"), _e.Utc(Escenario.Lunes, "18:00"), "Cerrado"));

        var r = await _e.Disponibilidad.DiasConHuecoAsync(Escenario.Empleado, Escenario.Lunes, new DateOnly(2026, 12, 1), [1]);

        Assert.Equal([new DateOnly(2026, 12, 1)], r.Dias);
    }

    [Fact]
    public async Task Hasta_anterior_a_desde_es_error()
        => await Assert.ThrowsAsync<DomainException>(() =>
            _e.Disponibilidad.DiasConHuecoAsync(Escenario.Empleado, Escenario.Lunes, Escenario.Hoy, [1]));

    [Fact]
    public async Task Un_rango_de_mas_de_62_dias_se_rechaza()
        => await Assert.ThrowsAsync<DomainException>(() =>
            _e.Disponibilidad.DiasConHuecoAsync(Escenario.Empleado, Escenario.Lunes, Escenario.Lunes.AddDays(62), [1]));

    [Fact]
    public async Task Empleado_inexistente_es_no_encontrado()
        => await Assert.ThrowsAsync<NotFoundAppException>(() =>
            _e.Disponibilidad.HorasLibresAsync(999, Escenario.Lunes, [1]));

    [Fact]
    public async Task Empleado_inactivo_es_no_encontrado()
        => await Assert.ThrowsAsync<NotFoundAppException>(() =>
            _e.Disponibilidad.HorasLibresAsync(Escenario.EmpleadoInactivo, Escenario.Lunes, [1]));

    [Fact]
    public async Task Servicio_no_agendable_es_error_de_validacion()
        => await Assert.ThrowsAsync<DomainException>(() => Horas(Escenario.Lunes, 4));

    [Fact]
    public async Task Sin_servicios_es_error_de_validacion()
        => await Assert.ThrowsAsync<DomainException>(() =>
            _e.Disponibilidad.HorasLibresAsync(Escenario.Empleado, Escenario.Lunes, []));

    [Fact]
    public async Task Lo_que_ofrece_la_disponibilidad_siempre_pasa_la_validacion()
    {
        // Coherencia: cada hora ofrecida debe aceptarse al agendar (mismos criterios).
        await _e.AgendarAsync(Escenario.Lunes, "10:00");
        var horas = await Horas(Escenario.Lunes);

        foreach (var hora in horas)
        {
            var ini = _e.Utc(Escenario.Lunes, hora);
            await _e.Reglas.ValidarHuecoAsync(Escenario.Empleado, ini, ini.AddMinutes(30), null, default);
        }
    }
}
