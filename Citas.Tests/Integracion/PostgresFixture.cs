using Citas.Application.Acceso.Abstractions;
using Citas.Application.Agendamiento;
using Citas.Application.Calendario;
using Citas.Application.Common;
using Citas.Data;
using Citas.Domain.Acceso;
using Citas.Domain.Agendamiento;
using Citas.Domain.Calendario;
using Citas.Domain.Empresas;
using Citas.Domain.Servicios;
using Citas.Infrastructure.Acceso;
using Citas.Infrastructure.Agendamiento;
using Citas.Infrastructure.Calendario;
using Citas.Infrastructure.Common;
using Citas.Infrastructure.Empresas;
using Citas.Infrastructure.Servicios;
using Citas.Models;
using Citas.Multitenancy;
using Citas.Tests.Soporte;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.PostgreSql;

namespace Citas.Tests.Integracion;

/// <summary>
/// Un PostgreSQL real y desechable (contenedor Docker) con el esquema creado por las
/// migraciones del proyecto: el mismo motor y la misma restricción anti-cruces que en
/// producción. Se comparte entre todas las pruebas de integración; cada prueba usa su
/// propio tenant, así que no se estorban.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder("postgres:17").Build();

    public string Cadena => _pg.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _pg.StartAsync();
        await using var db = CrearContexto("migraciones");
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _pg.DisposeAsync().AsTask();

    public CitasContext CrearContexto(string tenant)
        => new(new DbContextOptionsBuilder<CitasContext>().UseNpgsql(Cadena).Options, new TenantFijo(tenant));

    /// <summary>Un negocio nuevo (tenant propio) con su barbero, horario, servicios y clientes.</summary>
    public async Task<Negocio> CrearNegocioAsync(int clientes = 12)
    {
        var negocio = new Negocio(this, "t-" + Guid.NewGuid().ToString("N")[..12]);
        await negocio.SembrarAsync(clientes);
        return negocio;
    }
}

public sealed class TenantFijo(string tenant) : ITenantProvider
{
    public string? GetTenantId() => tenant;
}

[CollectionDefinition(Nombre)]
public sealed class ColeccionPostgres : ICollectionFixture<PostgresFixture>
{
    public const string Nombre = "postgres";
}

/// <summary>Un tenant dentro de la BD de pruebas, con el contenedor de servicios para atenderlo.</summary>
public sealed class Negocio
{
    public static readonly DateOnly Lunes = Escenario.Lunes;

    private readonly ServiceProvider _servicios;

    public string Tenant { get; }
    public RelojAgenda Reloj { get; }
    public int Empleado { get; private set; }
    public int ServicioCorte { get; private set; }
    public List<int> Clientes { get; } = [];

    /// <summary>El Google Calendar de mentira que usa el sincronizador de este negocio.</summary>
    public CalendarioExternoFalso Google { get; } = new();
    public RelojFalso Tiempo { get; }

    public Negocio(PostgresFixture bd, string tenant)
    {
        Tenant = tenant;
        var tiempo = new RelojFalso(new DateTimeOffset(2026, 11, 25, 15, 0, 0, TimeSpan.Zero));
        Tiempo = tiempo;
        var opciones = new OpcionesAgenda { ZonaHoraria = OpcionesAgenda.ResolverZona("America/Bogota") };

        var s = new ServiceCollection();
        s.AddSingleton<ITenantProvider>(new TenantFijo(tenant));
        s.AddDbContext<CitasContext>(o => o.UseNpgsql(bd.Cadena));
        s.AddScoped<ICitaRepository, CitaRepository>();
        s.AddScoped<IAgendaEmpleadoRepository, AgendaEmpleadoRepository>();
        s.AddScoped<IEmpresaRepository, EmpresaRepository>();
        s.AddScoped<IServicioRepository, ServicioRepository>();
        s.AddScoped<IUsuarioRepository, UsuarioRepository>();
        s.AddScoped<IUnitOfWork, UnitOfWork>();
        s.AddSingleton(opciones);
        s.AddSingleton<TimeProvider>(tiempo);
        s.AddSingleton<RelojAgenda>();
        s.AddScoped<IUsuarioActual>(_ => new UsuarioActualFalso { Tenant = tenant, RolNatural = RolesNatural.Administrador });
        s.AddScoped<ReglasAgenda>();
        s.AddScoped<CitaAppService>();
        s.AddScoped<DisponibilidadAppService>();

        // Integración con Google Calendar: repositorio real, Google y cifrado de mentira.
        var opcionesGoogle = new OpcionesGoogle { ClientId = "id", ClaveCifradoBase64 = Convert.ToBase64String(new byte[32]) };
        s.AddSingleton(opcionesGoogle);
        s.AddSingleton(new FirmasGoogle(opcionesGoogle));
        s.AddSingleton<ICalendarioExterno>(Google);
        s.AddSingleton<ICifradorSecretos>(new CifradorFalso());
        s.AddLogging();
        s.AddScoped<ICalendarioRepositorio, CalendarioRepositorio>();
        s.AddScoped<SincronizadorCalendario>();
        _servicios = s.BuildServiceProvider();
        Reloj = _servicios.GetRequiredService<RelojAgenda>();
    }

    /// <summary>Un alcance nuevo = una petición HTTP nueva (su propio DbContext y transacción).</summary>
    public IServiceScope Peticion() => _servicios.CreateScope();

    public CreateCitaDto Dto(string hora, int idCliente, DateOnly? fecha = null)
        => new()
        {
            IdCliente = idCliente,
            IdEmpleado = Empleado,
            FechaCita = (fecha ?? Lunes).ToDateTime(TimeOnly.MinValue),
            HoraEstimadaCita = TimeOnly.Parse(hora).ToTimeSpan(),
            IdServicios = [ServicioCorte],
        };

    public async Task SembrarAsync(int clientes)
    {
        using var peticion = Peticion();
        var p = peticion.ServiceProvider;
        var uow = p.GetRequiredService<IUnitOfWork>();

        await p.GetRequiredService<IEmpresaRepository>().AgregarAsync(Empresa.Crear(Tenant, "Negocio " + Tenant));

        var barbero = Usuario.CrearParaPersonalNatural("barbero", 501);
        var usuarios = p.GetRequiredService<IUsuarioRepository>();
        await usuarios.AgregarAsync(barbero, Tenant);
        var listaClientes = new List<Usuario>();
        for (var i = 0; i < clientes; i++)
        {
            var cliente = Usuario.Crear($"cliente-{i}");
            listaClientes.Add(cliente);
            await usuarios.AgregarAsync(cliente, Tenant);
        }

        var corte = Servicio.CrearDesdeNatural(101, "Corte", 20000m, true, 30);
        await p.GetRequiredService<IServicioRepository>().AgregarAsync(corte);
        await uow.GuardarCambiosAsync();

        Empleado = barbero.Id;
        ServicioCorte = corte.IdServicio;
        Clientes.AddRange(listaClientes.Select(c => c.Id));

        await p.GetRequiredService<IAgendaEmpleadoRepository>().ReemplazarHorarioAsync(
            AgendaEmpleado.Definir(Empleado, Escenario.LunesAViernes(
                (new TimeOnly(9, 0), new TimeOnly(12, 0)),
                (new TimeOnly(14, 0), new TimeOnly(18, 0)))));
        await uow.GuardarCambiosAsync();
    }
}
