using Citas.Domain.Common;

namespace Citas.Models;

// Identidad global del cliente (la misma persona puede entrar a varias empresas).
// No pertenece a ningún tenant. La tabla se creó a mano con columnas en minúscula;
// ese detalle vive en el modelo de persistencia, no aquí.
public class Persona
{
    private Persona() { }

    public int Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Apellido { get; private set; } = string.Empty;
    public string? Cedula { get; private set; }
    public string? Residencia { get; private set; }
    public string? Email { get; private set; }
    public int? Edad { get; private set; }
    public string? Celular { get; private set; }
    public DateTime? FechaNacimiento { get; private set; }

    // Hash de la contraseña del login global (PBKDF2).
    public string? ClaveHash { get; private set; }

    // Registra una persona nueva. La clave ya debe llegar hasheada desde la app.
    public static Persona Registrar(
        string nombre,
        string? apellido,
        string email,
        string? celular,
        string claveHash)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre es obligatorio.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("El email es obligatorio.");
        if (string.IsNullOrWhiteSpace(claveHash))
            throw new DomainException("La contraseña es obligatoria.");

        return new Persona
        {
            Nombre = nombre.Trim(),
            Apellido = (apellido ?? string.Empty).Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Celular = string.IsNullOrWhiteSpace(celular) ? null : celular.Trim(),
            ClaveHash = claveHash,
        };
    }

    // Reconstruye una persona existente desde la base de datos.
    public static Persona Rehidratar(
        int id,
        string nombre,
        string apellido,
        string? cedula,
        string? residencia,
        string? email,
        int? edad,
        string? celular,
        DateTime? fechaNacimiento,
        string? claveHash)
        => new()
        {
            Id = id,
            Nombre = nombre,
            Apellido = apellido,
            Cedula = cedula,
            Residencia = residencia,
            Email = email,
            Edad = edad,
            Celular = celular,
            FechaNacimiento = fechaNacimiento,
            ClaveHash = claveHash,
        };

    // La persistencia asigna el id real una sola vez, tras el primer guardado.
    public void EstablecerIdentidad(int id)
    {
        if (Id == 0) Id = id;
    }

    // Nombre completo para mostrar.
    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
}
