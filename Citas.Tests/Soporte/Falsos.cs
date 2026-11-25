using Citas.Application.Acceso.Abstractions;
using Citas.Application.Common;
using Citas.Domain.Acceso;
using Citas.Domain.Agendamiento;
using Citas.Domain.Servicios;
using Citas.Models;

namespace Citas.Tests.Soporte;

/// <summary>Reloj que solo avanza cuando la prueba lo pide.</summary>
public sealed class RelojFalso : TimeProvider
{
    private DateTimeOffset _ahora;

    public RelojFalso(DateTimeOffset ahora) => _ahora = ahora;

    public override DateTimeOffset GetUtcNow() => _ahora;

    public void Avanzar(TimeSpan tiempo) => _ahora += tiempo;
}

/// <summary>Quién hace la petición: la prueba lo fija para probar permisos.</summary>
public sealed class UsuarioActualFalso : IUsuarioActual
{
    public ModoAcceso Modo { get; set; } = ModoAcceso.Personal;
    public string? Tenant { get; set; } = "T1";
    public int? UsuarioId { get; set; }
    public int? PersonaId { get; set; }
    public int? PersonaNaturalId { get; set; }
    public string? RolNatural { get; set; } = RolesNatural.Administrador;
    public string? Nombre { get; set; } = "prueba";
}

public sealed class UnidadTrabajoMemoria : IUnitOfWork
{
    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task<T> EjecutarEnTransaccionAsync<T>(
        Func<CancellationToken, Task<T>> operacion,
        CancellationToken cancellationToken = default) => operacion(cancellationToken);

    public void DescartarCambios() { }
}

public sealed class RepositorioServiciosMemoria : IServicioRepository
{
    public List<Servicio> Datos { get; } = [];

    public Task<IReadOnlyList<Servicio>> ListarAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Servicio>>(Datos.ToList());

    public Task<Servicio?> ObtenerPorIdAsync(int idServicio, CancellationToken cancellationToken = default)
        => Task.FromResult(Datos.FirstOrDefault(s => s.IdServicio == idServicio));

    public Task<IReadOnlyList<Servicio>> ObtenerPorIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Servicio>>(Datos.Where(s => ids.Contains(s.IdServicio)).ToList());

    public Task AgregarAsync(Servicio servicio, CancellationToken cancellationToken = default)
    {
        Datos.Add(servicio);
        return Task.CompletedTask;
    }

    public void Actualizar(Servicio servicio) { }
}

public sealed class RepositorioUsuariosMemoria : IUsuarioRepository
{
    public List<Usuario> Datos { get; } = [];

    public Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        => Task.FromResult(Datos.FirstOrDefault(u => u.Id == id));

    public Task<IReadOnlyList<Usuario>> ListarConRolesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Usuario>>(Datos.ToList());

    public Task AgregarAsync(Usuario usuario, string? tenant, CancellationToken cancellationToken = default)
    {
        Datos.Add(usuario);
        return Task.CompletedTask;
    }

    public void Eliminar(Usuario usuario) => Datos.Remove(usuario);

    public void Actualizar(Usuario usuario) { }

    public Task<IReadOnlyList<Usuario>> ListarPersonalNaturalAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Usuario>>(Datos.Where(u => u.EsPersonalNatural).ToList());

    public Task<Usuario?> BuscarEnTenantPorPersonaAsync(int personaId, string tenant, CancellationToken cancellationToken = default)
        => Task.FromResult(Datos.FirstOrDefault(u => u.PersonaId == personaId));

    public Task<Usuario?> BuscarEnTenantPorPersonaNaturalAsync(string tenant, int personaNaturalId, CancellationToken cancellationToken = default)
        => Task.FromResult(Datos.FirstOrDefault(u => u.PersonaNaturalId == personaNaturalId));
}

public sealed class RepositorioCitasMemoria : ICitaRepository
{
    private int _siguiente = 1;

    public List<Cita> Datos { get; } = [];
    public RepositorioUsuariosMemoria Usuarios { get; }

    public RepositorioCitasMemoria(RepositorioUsuariosMemoria usuarios) => Usuarios = usuarios;

    public Task<IReadOnlyList<Cita>> ListarAsync(int? clienteId = null, int? empleadoId = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Cita>>(Datos
            .Where(c => clienteId is null || c.IdCliente == clienteId)
            .Where(c => empleadoId is null || c.IdEmpleado == empleadoId)
            .ToList());

    public Task<IReadOnlyList<Cita>> ListarActivasDeEmpleadoAsync(
        int idEmpleado, DateTime desdeUtc, DateTime hastaUtc, int? excluirIdCita = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Cita>>(Datos
            .Where(c => c.IdEmpleado == idEmpleado && c.EstaActiva && c.IdCita != excluirIdCita)
            .Where(c => c.Inicio < hastaUtc && desdeUtc < c.Fin)
            .OrderBy(c => c.Inicio)
            .ToList());

    public Task<IReadOnlyList<Cita>> ListarParaCalendarioAsync(
        int idEmpleado, DateTime desdeUtc, IReadOnlyCollection<int> idsPublicadas, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Cita>>(Datos
            .Where(c => (c.IdEmpleado == idEmpleado && c.Fin >= desdeUtc) || idsPublicadas.Contains(c.IdCita))
            .OrderBy(c => c.Inicio)
            // Como la base de datos real: la cita llega con los nombres del cliente y del empleado.
            .Select(c => Cita.Rehidratar(c.IdCita, c.IdCliente, c.IdEmpleado, c.Estado, c.Inicio, c.Fin, c.Observaciones,
                Usuarios.Datos.FirstOrDefault(u => u.Id == c.IdCliente), Usuarios.Datos.FirstOrDefault(u => u.Id == c.IdEmpleado), c.Detalles))
            .ToList());

    public Task<Cita?> ObtenerPorIdAsync(int idCita, CancellationToken cancellationToken = default)
        => Task.FromResult(Datos.FirstOrDefault(c => c.IdCita == idCita));

    public Task<bool> ExisteUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default)
        => Task.FromResult(Usuarios.Datos.Any(u => u.Id == usuarioId));

    public Task AgregarAsync(Cita cita, CancellationToken cancellationToken = default)
    {
        cita.EstablecerIdentidad(_siguiente++);
        Datos.Add(cita);
        return Task.CompletedTask;
    }

    public void Actualizar(Cita cita) { }

    public void Eliminar(Cita cita) => Datos.Remove(cita);
}

public sealed class RepositorioAgendaMemoria : IAgendaEmpleadoRepository
{
    private readonly Dictionary<int, AgendaEmpleado> _horarios = [];

    private int _siguienteBloqueo = 1;

    public List<BloqueoAgenda> Bloqueos { get; } = [];

    public Task<AgendaEmpleado> ObtenerHorarioAsync(int idEmpleado, CancellationToken cancellationToken = default)
        => Task.FromResult(_horarios.TryGetValue(idEmpleado, out var h) ? h : AgendaEmpleado.Rehidratar(idEmpleado, []));

    public Task ReemplazarHorarioAsync(AgendaEmpleado agenda, CancellationToken cancellationToken = default)
    {
        _horarios[agenda.IdEmpleado] = agenda;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BloqueoAgenda>> ListarBloqueosAsync(
        int idEmpleado, DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BloqueoAgenda>>(Bloqueos
            .Where(b => b.IdEmpleado == idEmpleado && b.Inicio < hastaUtc && desdeUtc < b.Fin)
            .OrderBy(b => b.Inicio)
            .ToList());

    public Task<BloqueoAgenda?> ObtenerBloqueoAsync(int idEmpleado, int idBloqueo, CancellationToken cancellationToken = default)
        => Task.FromResult(Bloqueos.FirstOrDefault(b => b.IdEmpleado == idEmpleado && b.IdBloqueo == idBloqueo));

    public Task AgregarBloqueoAsync(BloqueoAgenda bloqueo, CancellationToken cancellationToken = default)
    {
        bloqueo.EstablecerIdentidad(_siguienteBloqueo++);
        Bloqueos.Add(bloqueo);
        return Task.CompletedTask;
    }

    public void EliminarBloqueo(BloqueoAgenda bloqueo) => Bloqueos.Remove(bloqueo);

    public Task BloquearAgendaAsync(int idEmpleado, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
