using Citas.Models;

namespace Citas.Domain.Agendamiento;

/// <summary>
/// Repositorio del agregado Cita. A diferencia de un repositorio genérico, expone
/// solo las operaciones que el dominio necesita y trabaja siempre con la raíz de
/// agregado (Cita) completa. La implementación vive en la capa de infraestructura.
/// </summary>
public interface ICitaRepository
{
    /// <summary>Lista las citas del tenant, opcionalmente filtradas por cliente o empleado.</summary>
    Task<IReadOnlyList<Cita>> ListarAsync(
        int? clienteId = null,
        int? empleadoId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Obtiene una cita por su identificador, o null si no existe en el tenant.</summary>
    Task<Cita?> ObtenerPorIdAsync(int idCita, CancellationToken cancellationToken = default);

    /// <summary>Indica si existe un usuario (cliente o empleado) con ese id en el tenant.</summary>
    Task<bool> ExisteUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default);

    Task AgregarAsync(Cita cita, CancellationToken cancellationToken = default);

    void Eliminar(Cita cita);
}
