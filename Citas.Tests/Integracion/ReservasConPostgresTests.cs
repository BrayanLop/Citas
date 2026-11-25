using Citas.Application.Agendamiento;
using Citas.Domain.Agendamiento;
using Citas.Domain.Common;
using Citas.Models;
using Citas.Tests.Soporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Citas.Tests.Integracion;

/// <summary>
/// Reglas de agenda contra PostgreSQL real. Son las que más duelen si fallan: dos clientes
/// reservando la misma hora a la vez. La defensa tiene dos capas (bloqueo por empleado dentro
/// de la transacción y restricción de exclusión en la tabla) y aquí se prueba cada una.
/// </summary>
[Collection(ColeccionPostgres.Nombre)]
public class ReservasConPostgresTests(PostgresFixture bd)
{
    /// <summary>Resultado de intentar agendar: la cita creada o el error que recibió el cliente.</summary>
    private sealed record Intento(string Hora, CitaResponseDto? Cita, Exception? Error);

    private static async Task<Intento> AgendarEnSuPropiaPeticionAsync(Negocio n, string hora, int idCliente)
    {
        using var peticion = n.Peticion();
        try
        {
            var cita = await peticion.ServiceProvider.GetRequiredService<CitaAppService>()
                .AgendarAsync(n.Dto(hora, idCliente));
            return new Intento(hora, cita, null);
        }
        catch (Exception e)
        {
            return new Intento(hora, null, e);
        }
    }

    // Lanza todas las peticiones a la vez (cada una en su hilo, con su DbContext).
    private static async Task<Intento[]> SimultaneasAsync(Negocio n, IReadOnlyList<(string Hora, int Cliente)> peticiones)
    {
        using var salida = new ManualResetEventSlim(false);
        var tareas = peticiones
            .Select(p => Task.Run(() =>
            {
                salida.Wait();
                return AgendarEnSuPropiaPeticionAsync(n, p.Hora, p.Cliente);
            }))
            .ToArray();
        await Task.Delay(300);   // todas listas esperando la salida
        salida.Set();
        return await Task.WhenAll(tareas);
    }

    private async Task<List<Cita>> CitasDelEmpleadoAsync(Negocio n)
    {
        using var peticion = n.Peticion();
        var citas = await peticion.ServiceProvider.GetRequiredService<ICitaRepository>().ListarAsync();
        return citas.Where(c => c.IdEmpleado == n.Empleado).ToList();
    }

    [Fact]
    public async Task La_migracion_crea_la_restriccion_que_impide_citas_cruzadas()
    {
        await using var db = bd.CrearContexto("verificacion");
        var existe = await db.Database
            .SqlQuery<int>($"SELECT count(*)::int AS \"Value\" FROM pg_constraint WHERE conname = 'EX_Citas_EmpleadoSinCruces'")
            .SingleAsync();

        Assert.Equal(1, existe);
    }

    [Fact]
    public async Task Doce_clientes_reservando_la_misma_hora_a_la_vez_solo_dejan_pasar_a_uno()
    {
        var n = await bd.CrearNegocioAsync(clientes: 12);

        var resultados = await SimultaneasAsync(n, n.Clientes.Select(c => ("10:00", c)).ToList());

        Assert.Single(resultados, r => r.Cita is not null);
        // Todos los demás reciben un conflicto limpio (409), no un 500 ni un bloqueo mutuo.
        Assert.All(resultados.Where(r => r.Cita is null),
            r => Assert.IsType<ConflictException>(r.Error));
        Assert.Single(await CitasDelEmpleadoAsync(n));
    }

    [Fact]
    public async Task Reservas_simultaneas_de_horas_contiguas_pasan_todas()
    {
        var n = await bd.CrearNegocioAsync(clientes: 4);
        string[] horas = ["10:00", "10:30", "11:00", "11:30"];

        var resultados = await SimultaneasAsync(n, horas.Select((h, i) => (h, n.Clientes[i])).ToList());

        Assert.All(resultados, r => Assert.True(r.Cita is not null, r.Error?.Message));
        Assert.Equal(4, (await CitasDelEmpleadoAsync(n)).Count);
    }

    [Fact]
    public async Task Con_horas_que_se_solapan_a_medias_las_aceptadas_nunca_se_cruzan()
    {
        // Cada cita dura 30 min y las horas de inicio van de 15 en 15: muchas se pisan.
        var n = await bd.CrearNegocioAsync(clientes: 8);
        string[] horas = ["10:00", "10:15", "10:30", "10:45", "11:00", "11:15", "11:30", "09:45"];

        var resultados = await SimultaneasAsync(n, horas.Select((h, i) => (h, n.Clientes[i])).ToList());

        Assert.All(resultados.Where(r => r.Cita is null), r => Assert.IsType<ConflictException>(r.Error));
        var aceptadas = (await CitasDelEmpleadoAsync(n)).OrderBy(c => c.Inicio).ToList();
        Assert.NotEmpty(aceptadas);
        for (var i = 1; i < aceptadas.Count; i++)
            Assert.True(aceptadas[i - 1].Fin <= aceptadas[i].Inicio,
                $"Se cruzan: {aceptadas[i - 1].Inicio:HH:mm}-{aceptadas[i - 1].Fin:HH:mm} y {aceptadas[i].Inicio:HH:mm}-{aceptadas[i].Fin:HH:mm}");
    }

    [Fact]
    public async Task Aunque_se_salte_la_validacion_la_base_de_datos_rechaza_el_cruce()
    {
        // Simula la peor carrera: dos citas llegan hasta el guardado sin pasar por ReglasAgenda.
        var n = await bd.CrearNegocioAsync(clientes: 2);
        var inicio = n.Reloj.AUtc(Negocio.Lunes, new TimeOnly(10, 0));
        var servicio = Servicio.Rehidratar(n.ServicioCorte, 101, "Corte", 20000m, 30, true, true);

        async Task Guardar(int cliente, DateTime desde)
        {
            using var peticion = n.Peticion();
            var citas = peticion.ServiceProvider.GetRequiredService<ICitaRepository>();
            await citas.AgregarAsync(Cita.Programar(cliente, n.Empleado, desde, null, [servicio]));
            await peticion.ServiceProvider.GetRequiredService<Citas.Application.Common.IUnitOfWork>().GuardarCambiosAsync();
        }

        await Guardar(n.Clientes[0], inicio);

        await Assert.ThrowsAsync<ConflictException>(() => Guardar(n.Clientes[1], inicio.AddMinutes(10)));
        await Guardar(n.Clientes[1], inicio.AddMinutes(30));   // contigua: sí entra
    }

    [Fact]
    public async Task Cancelar_una_cita_libera_la_hora_tambien_en_la_base_de_datos()
    {
        var n = await bd.CrearNegocioAsync(clientes: 2);

        // Cada operación va en su propia petición, como en la API real (una petición HTTP = un alcance).
        var cita = (await AgendarEnSuPropiaPeticionAsync(n, "10:00", n.Clientes[0])).Cita!;
        using (var peticion = n.Peticion())
        {
            await peticion.ServiceProvider.GetRequiredService<CitaAppService>().CambiarEstadoAsync(
                cita.IdCita, new PartialUpdateCitaDto { Estado = EstadoCita.Cancelada });
        }

        var nueva = await AgendarEnSuPropiaPeticionAsync(n, "10:00", n.Clientes[1]);
        Assert.NotNull(nueva.Cita);
    }

    [Fact]
    public async Task Reprogramar_sobre_otra_cita_da_conflicto_y_no_cambia_nada()
    {
        var n = await bd.CrearNegocioAsync(clientes: 2);
        await AgendarEnSuPropiaPeticionAsync(n, "10:00", n.Clientes[0]);
        var idSegunda = (await AgendarEnSuPropiaPeticionAsync(n, "11:00", n.Clientes[1])).Cita!.IdCita;

        using (var peticion = n.Peticion())
        {
            var app = peticion.ServiceProvider.GetRequiredService<CitaAppService>();
            await Assert.ThrowsAsync<ConflictException>(() =>
                app.ReprogramarAsync(idSegunda, new UpdateCitaDto { HoraEstimadaCita = new TimeSpan(10, 15, 0) }));
        }

        var guardada = (await CitasDelEmpleadoAsync(n)).Single(c => c.IdCita == idSegunda);
        Assert.Equal(n.Reloj.AUtc(Negocio.Lunes, new TimeOnly(11, 0)), guardada.Inicio);   // sigue donde estaba
    }

    [Fact]
    public async Task Las_citas_de_un_negocio_no_se_ven_desde_otro()
    {
        var a = await bd.CrearNegocioAsync(clientes: 1);
        var b = await bd.CrearNegocioAsync(clientes: 1);
        await AgendarEnSuPropiaPeticionAsync(a, "10:00", a.Clientes[0]);

        Assert.Single(await CitasDelEmpleadoAsync(a));
        using var peticionB = b.Peticion();
        Assert.Empty(await peticionB.ServiceProvider.GetRequiredService<ICitaRepository>().ListarAsync());
    }

    [Fact]
    public async Task Un_negocio_no_puede_agendar_con_el_empleado_de_otro()
    {
        var a = await bd.CrearNegocioAsync(clientes: 1);
        var b = await bd.CrearNegocioAsync(clientes: 1);

        using var peticion = b.Peticion();
        var dto = b.Dto("10:00", b.Clientes[0]);
        dto.IdEmpleado = a.Empleado;   // empleado del otro negocio

        await Assert.ThrowsAsync<DomainException>(() =>
            peticion.ServiceProvider.GetRequiredService<CitaAppService>().AgendarAsync(dto));
    }
}
