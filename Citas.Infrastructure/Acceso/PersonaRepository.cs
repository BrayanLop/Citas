using Citas.Data;
using Citas.Domain.Acceso;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Acceso;

public class PersonaRepository : IPersonaRepository
{
    private readonly CitasContext _db;

    public PersonaRepository(CitasContext db) => _db = db;

    public async Task<Persona?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        => await _db.Personas.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Persona?> ObtenerPorEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _db.Personas.FirstOrDefaultAsync(
            p => p.Email != null && p.Email.ToLower() == email, cancellationToken);

    public async Task<bool> ExistePorEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _db.Personas.AnyAsync(
            p => p.Email != null && p.Email.ToLower() == email, cancellationToken);

    public async Task AgregarAsync(Persona persona, CancellationToken cancellationToken = default)
        => await _db.Personas.AddAsync(persona, cancellationToken);
}
