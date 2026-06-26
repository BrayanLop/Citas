using Citas.Data;
using Citas.Domain.Acceso;
using Citas.Infrastructure.Persistencia;
using Citas.Infrastructure.Persistencia.Mapeadores;
using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Infrastructure.Acceso;

public class PersonaRepository : IPersonaRepository
{
    private readonly CitasContext _db;

    public PersonaRepository(CitasContext db) => _db = db;

    public async Task<Persona?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var fila = await _db.Personas.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return fila is null ? null : PersonaMapper.ToDomain(fila);
    }

    public async Task<Persona?> ObtenerPorEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var fila = await _db.Personas.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Email != null && p.Email.ToLower() == email, cancellationToken);
        return fila is null ? null : PersonaMapper.ToDomain(fila);
    }

    public Task<bool> ExistePorEmailAsync(string email, CancellationToken cancellationToken = default)
        => _db.Personas.AsNoTracking()
            .AnyAsync(p => p.Email != null && p.Email.ToLower() == email, cancellationToken);

    public async Task AgregarAsync(Persona persona, CancellationToken cancellationToken = default)
    {
        var fila = PersonaMapper.ToDbModel(persona);
        await _db.Personas.AddAsync(fila, cancellationToken);
        _db.SincronizarIdTrasGuardar(() => persona.EstablecerIdentidad(fila.Id));
    }
}
