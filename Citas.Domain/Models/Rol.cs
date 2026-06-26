using Citas.Domain.Common;

namespace Citas.Models;

// Rol dentro de una empresa (Cliente, Empleado, ...).
public class Rol
{
    private Rol() { }

    public int Id { get; private set; }
    public string NombreRol { get; private set; } = string.Empty;

    public static Rol Crear(string nombreRol)
    {
        if (string.IsNullOrWhiteSpace(nombreRol))
            throw new DomainException("El nombre del rol es obligatorio.");
        return new Rol { NombreRol = nombreRol.Trim() };
    }

    public static Rol Rehidratar(int id, string nombreRol)
        => new() { Id = id, NombreRol = nombreRol };

    // La persistencia asigna el id real una sola vez, tras el primer guardado.
    public void EstablecerIdentidad(int id)
    {
        if (Id == 0) Id = id;
    }
}
