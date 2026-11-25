using Citas.Domain.Agendamiento;
using Citas.Domain.Common;
using Citas.Models;
using Citas.Tests.Soporte;

namespace Citas.Tests.Dominio;

public class EstadoCitaTests
{
    [Theory]
    [InlineData("Pendiente", "Confirmada", true)]
    [InlineData("Pendiente", "Cancelada", true)]
    [InlineData("Confirmada", "Atendida", true)]
    [InlineData("Confirmada", "Cancelada", true)]
    [InlineData("Pendiente", "Atendida", false)]     // no se atiende sin confirmar
    [InlineData("Atendida", "Cancelada", false)]     // terminal
    [InlineData("Cancelada", "Pendiente", false)]    // terminal
    [InlineData("Cancelada", "Confirmada", false)]
    public void Transiciones_permitidas_y_prohibidas(string desde, string hacia, bool esperado)
        => Assert.Equal(esperado, EstadoCita.PuedeTransicionar(desde, hacia));

    [Theory]
    [InlineData("Pendiente", true)]
    [InlineData("Confirmada", true)]
    [InlineData("Atendida", false)]
    [InlineData("Cancelada", false)]
    [InlineData("Otro", false)]
    [InlineData(null, false)]
    public void Solo_pendiente_y_confirmada_ocupan_agenda(string? estado, bool esperado)
        => Assert.Equal(esperado, EstadoCita.EsActivo(estado));
}

public class CitaTests
{
    private static readonly DateTime Inicio = new(2026, 11, 30, 15, 0, 0, DateTimeKind.Utc);

    private static Servicio Corte => Servicio.Rehidratar(1, 101, "Corte", 20000m, 30, true, true);
    private static Servicio Barba => Servicio.Rehidratar(2, 102, "Barba", 12000m, 15, true, true);
    private static Servicio Retirado => Servicio.Rehidratar(4, 104, "Retirado", 5000m, 30, true, false);

    [Fact]
    public void Programar_calcula_el_fin_con_la_suma_de_duraciones_y_el_valor_total()
    {
        var cita = Cita.Programar(20, 10, Inicio, null, [Corte, Barba]);

        Assert.Equal(Inicio.AddMinutes(45), cita.Fin);
        Assert.Equal(45, cita.DuracionTotal);
        Assert.Equal(32000m, cita.ValorTotal);
        Assert.Equal(EstadoCita.Pendiente, cita.Estado);
        Assert.True(cita.EstaActiva);
    }

    [Fact]
    public void Programar_guarda_una_foto_del_valor_y_la_duracion_del_servicio()
    {
        var cita = Cita.Programar(20, 10, Inicio, null, [Corte]);
        var linea = Assert.Single(cita.Detalles);

        Assert.Equal(20000m, linea.ValorServicio);
        Assert.Equal(30, linea.TiempoEstimado);
    }

    [Fact]
    public void Programar_sin_servicios_falla()
        => Assert.Throws<DomainException>(() => Cita.Programar(20, 10, Inicio, null, []));

    [Fact]
    public void Programar_con_servicio_no_agendable_falla()
        => Assert.Throws<DomainException>(() => Cita.Programar(20, 10, Inicio, null, [Retirado]));

    [Fact]
    public void Programar_con_servicio_repetido_falla()
        => Assert.Throws<DomainException>(() => Cita.Programar(20, 10, Inicio, null, [Corte, Corte]));

    [Fact]
    public void Cliente_y_empleado_no_pueden_ser_la_misma_persona()
        => Assert.Throws<DomainException>(() => Cita.Programar(10, 10, Inicio, null, [Corte]));

    [Fact]
    public void El_inicio_debe_venir_en_utc()
        => Assert.Throws<ArgumentException>(() =>
            Cita.Programar(20, 10, new DateTime(2026, 11, 30, 10, 0, 0, DateTimeKind.Unspecified), null, [Corte]));

    [Fact]
    public void Camino_normal_pendiente_confirmada_atendida()
    {
        var cita = Cita.Programar(20, 10, Inicio, null, [Corte]);
        cita.Confirmar();
        Assert.Equal(EstadoCita.Confirmada, cita.Estado);
        cita.Atender();
        Assert.Equal(EstadoCita.Atendida, cita.Estado);
        Assert.False(cita.EstaActiva);
    }

    [Fact]
    public void Una_cita_cancelada_no_vuelve_a_activarse()
    {
        var cita = Cita.Programar(20, 10, Inicio, null, [Corte]);
        cita.Cancelar();
        Assert.Throws<DomainException>(() => cita.Confirmar());
    }

    [Fact]
    public void Estado_desconocido_falla()
    {
        var cita = Cita.Programar(20, 10, Inicio, null, [Corte]);
        Assert.Throws<DomainException>(() => cita.CambiarEstadoA("Inventado"));
    }

    [Fact]
    public void Pasar_al_mismo_estado_no_hace_nada()
    {
        var cita = Cita.Programar(20, 10, Inicio, null, [Corte]);
        cita.CambiarEstadoA(EstadoCita.Pendiente);
        Assert.Equal(EstadoCita.Pendiente, cita.Estado);
    }

    [Fact]
    public void Reprogramar_mueve_el_inicio_y_recalcula_el_fin()
    {
        var cita = Cita.Programar(20, 10, Inicio, null, [Corte]);
        var nuevo = Inicio.AddDays(1);

        cita.Reprogramar(10, nuevo, null);

        Assert.Equal(nuevo, cita.Inicio);
        Assert.Equal(nuevo.AddMinutes(30), cita.Fin);
    }

    [Fact]
    public void Reprogramar_con_servicios_nuevos_cambia_la_duracion()
    {
        var cita = Cita.Programar(20, 10, Inicio, null, [Corte]);

        cita.Reprogramar(10, Inicio, [Corte, Barba]);

        Assert.Equal(45, cita.DuracionTotal);
        Assert.Equal(Inicio.AddMinutes(45), cita.Fin);
        Assert.True(cita.DetallesReemplazados);
    }

    [Theory]
    [InlineData("Atendida")]
    [InlineData("Cancelada")]
    public void Una_cita_terminada_no_se_reprograma(string estadoFinal)
    {
        var cita = Cita.Programar(20, 10, Inicio, null, [Corte]);
        if (estadoFinal == "Atendida")
        {
            cita.Confirmar();
            cita.Atender();
        }
        else
        {
            cita.Cancelar();
        }

        Assert.Throws<DomainException>(() => cita.Reprogramar(10, Inicio.AddDays(1), null));
    }
}

public class AgendaEmpleadoTests
{
    private static FranjaHoraria Franja(DayOfWeek dia, string inicio, string fin)
        => new(dia, TimeOnly.Parse(inicio), TimeOnly.Parse(fin));

    [Fact]
    public void Acepta_manana_y_tarde_con_pausa_en_medio()
    {
        var agenda = AgendaEmpleado.Definir(10,
            [Franja(DayOfWeek.Monday, "09:00", "12:00"), Franja(DayOfWeek.Monday, "14:00", "18:00")]);

        Assert.Equal(2, agenda.Franjas.Count);
    }

    [Fact]
    public void Franjas_contiguas_no_se_consideran_solape()
        => AgendaEmpleado.Definir(10,
            [Franja(DayOfWeek.Monday, "09:00", "12:00"), Franja(DayOfWeek.Monday, "12:00", "14:00")]);

    [Fact]
    public void Franjas_que_se_solapan_se_rechazan()
        => Assert.Throws<DomainException>(() => AgendaEmpleado.Definir(10,
            [Franja(DayOfWeek.Monday, "09:00", "12:00"), Franja(DayOfWeek.Monday, "11:00", "14:00")]));

    [Theory]
    [InlineData("12:00", "12:00")]
    [InlineData("12:00", "09:00")]
    public void El_inicio_debe_ser_anterior_al_fin(string inicio, string fin)
        => Assert.Throws<DomainException>(() => AgendaEmpleado.Definir(10, [Franja(DayOfWeek.Monday, inicio, fin)]));

    [Fact]
    public void Sin_franjas_el_empleado_no_trabaja()
    {
        var agenda = AgendaEmpleado.Definir(10, []);
        Assert.False(agenda.Cubre(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(10, 30)));
    }

    [Fact]
    public void Cubre_solo_si_el_tramo_cabe_completo_en_una_franja()
    {
        var agenda = AgendaEmpleado.Definir(10,
            [Franja(DayOfWeek.Monday, "09:00", "12:00"), Franja(DayOfWeek.Monday, "14:00", "18:00")]);

        Assert.True(agenda.Cubre(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(9, 30)));
        Assert.True(agenda.Cubre(DayOfWeek.Monday, new TimeOnly(11, 30), new TimeOnly(12, 0)));   // fin exacto
        Assert.False(agenda.Cubre(DayOfWeek.Monday, new TimeOnly(11, 45), new TimeOnly(12, 15))); // se pasa
        Assert.False(agenda.Cubre(DayOfWeek.Monday, new TimeOnly(11, 30), new TimeOnly(14, 30))); // cruza el almuerzo
        Assert.False(agenda.Cubre(DayOfWeek.Tuesday, new TimeOnly(10, 0), new TimeOnly(10, 30))); // otro día
    }
}

public class BloqueoAgendaTests
{
    private static readonly DateTime A = new(2026, 11, 30, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void El_fin_debe_ser_posterior_al_inicio()
        => Assert.Throws<DomainException>(() => BloqueoAgenda.Crear(10, A, A, null));

    [Fact]
    public void El_motivo_no_puede_superar_el_maximo()
        => Assert.Throws<DomainException>(() =>
            BloqueoAgenda.Crear(10, A, A.AddHours(1), new string('x', BloqueoAgenda.LongitudMaximaMotivo + 1)));

    [Fact]
    public void Un_motivo_en_blanco_se_guarda_como_nulo()
        => Assert.Null(BloqueoAgenda.Crear(10, A, A.AddHours(1), "   ").Motivo);

    [Fact]
    public void Exige_instantes_en_utc()
        => Assert.Throws<ArgumentException>(() =>
            BloqueoAgenda.Crear(10, new DateTime(2026, 11, 30, 10, 0, 0), A.AddHours(1), null));
}

public class RelojAgendaTests
{
    private readonly Escenario _e = new();

    [Fact]
    public void Hora_local_de_Bogota_es_utc_menos_cinco()
        => Assert.Equal(new DateTime(2026, 11, 30, 15, 0, 0, DateTimeKind.Utc), _e.Utc(Escenario.Lunes, "10:00"));

    [Fact]
    public void Ida_y_vuelta_conserva_la_hora_local()
    {
        var utc = _e.Utc(Escenario.Lunes, "16:45");
        Assert.Equal(new TimeSpan(16, 45, 0), _e.Reloj.ALocal(utc).TimeOfDay);
    }

    [Fact]
    public void Con_offset_muestra_menos_cinco()
        => Assert.Equal(TimeSpan.FromHours(-5), _e.Reloj.ConOffset(_e.Utc(Escenario.Lunes, "10:00")).Offset);

    [Fact]
    public void Un_instante_con_Z_se_respeta_tal_cual()
    {
        var utc = new DateTime(2026, 11, 30, 20, 0, 0, DateTimeKind.Utc);
        Assert.Equal(utc, _e.Reloj.AUtc(utc));
    }

    [Fact]
    public void Hoy_se_calcula_en_la_zona_del_negocio_no_en_utc()
    {
        // 2026-11-26 02:00 UTC todavía es 25 de noviembre 21:00 en Bogotá.
        _e.Tiempo.Avanzar(TimeSpan.FromHours(11));
        Assert.Equal(new DateOnly(2026, 11, 25), _e.Reloj.Hoy);
    }

    [Fact]
    public void El_dia_local_ocupa_veinticuatro_horas_utc_desplazadas()
    {
        var (desde, hasta) = _e.Reloj.DiaEnUtc(Escenario.Lunes);
        Assert.Equal(new DateTime(2026, 11, 30, 5, 0, 0, DateTimeKind.Utc), desde);
        Assert.Equal(TimeSpan.FromHours(24), hasta - desde);
    }

    [Fact]
    public void Una_zona_desconocida_distinta_de_la_por_defecto_falla_al_arrancar()
        => Assert.Throws<InvalidOperationException>(() => Citas.Application.Agendamiento.OpcionesAgenda.ResolverZona("Marte/Olympus"));
}
