using Citas.Domain.Agendamiento;
using Citas.Domain.Common;
using Citas.Application.Agendamiento;
using Citas.Tests.Soporte;

namespace Citas.Tests.Aplicacion;

/// <summary>Casos de uso de las citas: qué puede hacer cada rol y cómo responde el sistema.</summary>
public class CitaAppServiceTests
{
    private readonly Escenario _e = new();

    // --- Agendar -------------------------------------------------------------------------

    [Fact]
    public async Task El_administrador_agenda_una_cita_y_la_respuesta_viene_en_hora_local()
    {
        var cita = await _e.ComoAdministrador().CitasApp.AgendarAsync(
            _e.Dto(Escenario.Lunes, "10:00", Escenario.Cliente, Escenario.Empleado, 1, 2));

        Assert.Equal(EstadoCita.Pendiente, cita.Estado);
        Assert.Equal(Escenario.Lunes.ToDateTime(TimeOnly.MinValue), cita.FechaCita);
        Assert.Equal(new TimeSpan(10, 0, 0), cita.HoraEstimadaCita);
        Assert.Equal(new TimeSpan(10, 45, 0), cita.HoraEstimadaFin);   // corte 30 + barba 15
        Assert.Equal(TimeSpan.FromHours(-5), cita.Inicio.Offset);
        Assert.Equal(32000m, cita.ValorTotal);
        Assert.Equal(2, cita.Servicios.Count);
    }

    [Fact]
    public async Task El_cliente_agenda_para_si_mismo_aunque_no_mande_su_id()
    {
        var cita = await _e.ComoCliente(Escenario.Cliente).CitasApp.AgendarAsync(
            _e.Dto(Escenario.Lunes, "10:00", idCliente: 0));

        Assert.Equal(Escenario.Cliente, cita.IdCliente);
    }

    [Fact]
    public async Task El_cliente_no_puede_agendar_a_nombre_de_otro()
        => await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            _e.ComoCliente(Escenario.Cliente).CitasApp.AgendarAsync(
                _e.Dto(Escenario.Lunes, "10:00", idCliente: Escenario.OtroCliente)));

    [Fact]
    public async Task Un_cliente_inexistente_se_rechaza()
        => await Assert.ThrowsAsync<DomainException>(() =>
            _e.ComoAdministrador().CitasApp.AgendarAsync(_e.Dto(Escenario.Lunes, "10:00", idCliente: 999)));

    [Fact]
    public async Task Un_empleado_inactivo_se_rechaza()
        => await Assert.ThrowsAsync<DomainException>(() =>
            _e.ComoAdministrador().CitasApp.AgendarAsync(
                _e.Dto(Escenario.Lunes, "10:00", Escenario.Cliente, Escenario.EmpleadoInactivo)));

    [Fact]
    public async Task Un_cliente_no_puede_ser_el_empleado_de_la_cita()
        => await Assert.ThrowsAsync<DomainException>(() =>
            _e.ComoAdministrador().CitasApp.AgendarAsync(
                _e.Dto(Escenario.Lunes, "10:00", Escenario.Cliente, Escenario.OtroCliente)));

    [Fact]
    public async Task Sin_servicios_se_rechaza()
    {
        var dto = _e.Dto(Escenario.Lunes, "10:00");
        dto.IdServicios = [];
        await Assert.ThrowsAsync<DomainException>(() => _e.ComoAdministrador().CitasApp.AgendarAsync(dto));
    }

    [Fact]
    public async Task Un_servicio_no_agendable_se_rechaza()
        => await Assert.ThrowsAsync<DomainException>(() =>
            _e.ComoAdministrador().CitasApp.AgendarAsync(_e.Dto(Escenario.Lunes, "10:00", servicios: 4)));

    [Fact]
    public async Task Una_hora_fuera_del_dia_se_rechaza()
    {
        var dto = _e.Dto(Escenario.Lunes, "10:00");
        dto.HoraEstimadaCita = TimeSpan.FromHours(25);
        await Assert.ThrowsAsync<DomainException>(() => _e.ComoAdministrador().CitasApp.AgendarAsync(dto));
    }

    [Fact]
    public async Task Dos_citas_en_el_mismo_hueco_dan_conflicto()
    {
        await _e.AgendarAsync(Escenario.Lunes, "10:00");
        await Assert.ThrowsAsync<ConflictException>(() =>
            _e.ComoCliente(Escenario.OtroCliente).CitasApp.AgendarAsync(_e.Dto(Escenario.Lunes, "10:00", 0)));
    }

    [Fact]
    public async Task Citas_seguidas_sin_cruce_se_aceptan()
    {
        await _e.AgendarAsync(Escenario.Lunes, "10:00");
        await _e.AgendarAsync(Escenario.Lunes, "10:30", Escenario.OtroCliente);
        Assert.Equal(2, _e.Citas.Datos.Count);
    }

    [Fact]
    public async Task Fuera_del_horario_del_empleado_da_conflicto()
        => await Assert.ThrowsAsync<ConflictException>(() =>
            _e.ComoAdministrador().CitasApp.AgendarAsync(_e.Dto(Escenario.Lunes, "20:00")));

    [Fact]
    public async Task Cancelar_libera_el_hueco_para_otro_cliente()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00");
        await _e.ComoAdministrador().CitasApp.CambiarEstadoAsync(id, new PartialUpdateCitaDto { Estado = EstadoCita.Cancelada });

        await _e.AgendarAsync(Escenario.Lunes, "10:00", Escenario.OtroCliente);   // ahora sí cabe
    }

    // --- Reprogramar ---------------------------------------------------------------------

    [Fact]
    public async Task El_administrador_reprograma_a_otra_hora()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00");

        var cita = await _e.ComoAdministrador().CitasApp.ReprogramarAsync(id, new UpdateCitaDto
        {
            HoraEstimadaCita = new TimeSpan(11, 0, 0),
        });

        Assert.Equal(new TimeSpan(11, 0, 0), cita!.HoraEstimadaCita);
    }

    [Fact]
    public async Task Reprogramar_a_la_misma_hora_no_choca_consigo_misma()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00");

        var cita = await _e.ComoAdministrador().CitasApp.ReprogramarAsync(id, new UpdateCitaDto
        {
            HoraEstimadaCita = new TimeSpan(10, 0, 0),
            IdServicios = [1, 2],   // ahora dura 45 min, empalma con su propio hueco anterior
        });

        Assert.Equal(new TimeSpan(10, 45, 0), cita!.HoraEstimadaFin);
    }

    [Fact]
    public async Task Reprogramar_sobre_otra_cita_da_conflicto()
    {
        await _e.AgendarAsync(Escenario.Lunes, "10:00");
        var id = await _e.AgendarAsync(Escenario.Lunes, "11:00", Escenario.OtroCliente);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _e.ComoAdministrador().CitasApp.ReprogramarAsync(id, new UpdateCitaDto { HoraEstimadaCita = new TimeSpan(10, 15, 0) }));
    }

    [Fact]
    public async Task El_cliente_reprograma_su_cita_mientras_esta_pendiente()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00", Escenario.Cliente);

        var cita = await _e.ComoCliente(Escenario.Cliente).CitasApp.ReprogramarAsync(id,
            new UpdateCitaDto { HoraEstimadaCita = new TimeSpan(15, 0, 0) });

        Assert.Equal(new TimeSpan(15, 0, 0), cita!.HoraEstimadaCita);
    }

    [Fact]
    public async Task El_cliente_no_reprograma_la_cita_de_otro()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00", Escenario.OtroCliente);
        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            _e.ComoCliente(Escenario.Cliente).CitasApp.ReprogramarAsync(id, new UpdateCitaDto { HoraEstimadaCita = new TimeSpan(15, 0, 0) }));
    }

    [Fact]
    public async Task El_cliente_no_reprograma_una_cita_ya_confirmada()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00", Escenario.Cliente);
        await _e.ComoAdministrador().CitasApp.CambiarEstadoAsync(id, new PartialUpdateCitaDto { Estado = EstadoCita.Confirmada });

        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            _e.ComoCliente(Escenario.Cliente).CitasApp.ReprogramarAsync(id, new UpdateCitaDto { HoraEstimadaCita = new TimeSpan(15, 0, 0) }));
    }

    [Fact]
    public async Task El_trabajador_no_reprograma()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00");
        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            _e.ComoTrabajador(Escenario.Empleado).CitasApp.ReprogramarAsync(id, new UpdateCitaDto { HoraEstimadaCita = new TimeSpan(15, 0, 0) }));
    }

    [Fact]
    public async Task Reprogramar_una_cita_inexistente_devuelve_nulo()
        => Assert.Null(await _e.ComoAdministrador().CitasApp.ReprogramarAsync(999, new UpdateCitaDto()));

    // --- Cambiar estado ------------------------------------------------------------------

    [Fact]
    public async Task El_trabajador_cambia_el_estado_de_las_citas_que_atiende()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00");
        Assert.True(await _e.ComoTrabajador(Escenario.Empleado).CitasApp.CambiarEstadoAsync(
            id, new PartialUpdateCitaDto { Estado = EstadoCita.Confirmada }));
    }

    [Fact]
    public async Task El_trabajador_no_toca_las_citas_de_otro_empleado()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00");
        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            _e.ComoTrabajador(usuarioId: 77).CitasApp.CambiarEstadoAsync(id, new PartialUpdateCitaDto { Estado = EstadoCita.Confirmada }));
    }

    [Fact]
    public async Task El_cliente_solo_puede_cancelar_su_cita()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00", Escenario.Cliente);

        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            _e.ComoCliente(Escenario.Cliente).CitasApp.CambiarEstadoAsync(id, new PartialUpdateCitaDto { Estado = EstadoCita.Confirmada }));

        Assert.True(await _e.ComoCliente(Escenario.Cliente).CitasApp.CambiarEstadoAsync(
            id, new PartialUpdateCitaDto { Estado = EstadoCita.Cancelada }));
    }

    [Fact]
    public async Task El_cliente_no_cancela_la_cita_de_otro()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00", Escenario.OtroCliente);
        await Assert.ThrowsAsync<ForbiddenAppException>(() =>
            _e.ComoCliente(Escenario.Cliente).CitasApp.CambiarEstadoAsync(id, new PartialUpdateCitaDto { Estado = EstadoCita.Cancelada }));
    }

    [Fact]
    public async Task Una_transicion_invalida_se_rechaza()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00");
        await Assert.ThrowsAsync<DomainException>(() =>
            _e.ComoAdministrador().CitasApp.CambiarEstadoAsync(id, new PartialUpdateCitaDto { Estado = EstadoCita.Atendida }));
    }

    [Fact]
    public async Task Cambiar_el_estado_de_una_cita_inexistente_devuelve_falso()
        => Assert.False(await _e.ComoAdministrador().CitasApp.CambiarEstadoAsync(999, new PartialUpdateCitaDto { Estado = EstadoCita.Cancelada }));

    // --- Listar y eliminar ---------------------------------------------------------------

    [Fact]
    public async Task El_cliente_solo_ve_sus_citas_aunque_pida_las_de_otro()
    {
        await _e.AgendarAsync(Escenario.Lunes, "10:00", Escenario.Cliente);
        await _e.AgendarAsync(Escenario.Lunes, "11:00", Escenario.OtroCliente);

        var lista = await _e.ComoCliente(Escenario.Cliente).CitasApp.ListarAsync(clienteId: Escenario.OtroCliente);

        Assert.All(lista, c => Assert.Equal(Escenario.Cliente, c.IdCliente));
        Assert.Single(lista);
    }

    [Fact]
    public async Task El_personal_ve_todas_las_citas_y_puede_filtrar()
    {
        await _e.AgendarAsync(Escenario.Lunes, "10:00", Escenario.Cliente);
        await _e.AgendarAsync(Escenario.Lunes, "11:00", Escenario.OtroCliente);

        Assert.Equal(2, (await _e.ComoAdministrador().CitasApp.ListarAsync()).Count);
        Assert.Single(await _e.ComoAdministrador().CitasApp.ListarAsync(clienteId: Escenario.OtroCliente));
    }

    [Fact]
    public async Task Eliminar_una_cita_existente_y_una_inexistente()
    {
        var id = await _e.AgendarAsync(Escenario.Lunes, "10:00");

        Assert.True(await _e.ComoAdministrador().CitasApp.EliminarAsync(id));
        Assert.False(await _e.ComoAdministrador().CitasApp.EliminarAsync(id));
    }
}
