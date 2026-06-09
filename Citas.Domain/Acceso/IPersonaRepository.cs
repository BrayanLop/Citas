using Citas.Models;

namespace Citas.Domain.Acceso;

/// <summary>Repositorio del agregado Persona (identidad global; contexto Acceso).</summary>
public interface IPersonaRepository
{
    Task<Persona?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Persona?> ObtenerPorEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<bool> ExistePorEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AgregarAsync(Persona persona, CancellationToken cancellationToken = default);
}
