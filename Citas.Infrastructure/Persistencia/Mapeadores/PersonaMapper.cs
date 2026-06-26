using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Models;

namespace Citas.Infrastructure.Persistencia.Mapeadores;

internal static class PersonaMapper
{
    public static Persona ToDomain(PersonaDbModel db)
        => Persona.Rehidratar(
            db.Id, db.Nombre, db.Apellido, db.Cedula, db.Residencia,
            db.Email, db.Edad, db.Celular, db.FechaNacimiento, db.ClaveHash);

    public static PersonaDbModel ToDbModel(Persona p)
        => new()
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Apellido = p.Apellido,
            Cedula = p.Cedula,
            Residencia = p.Residencia,
            Email = p.Email,
            Edad = p.Edad,
            Celular = p.Celular,
            FechaNacimiento = p.FechaNacimiento,
            ClaveHash = p.ClaveHash,
        };
}
