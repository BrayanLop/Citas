using Citas.Domain.Common;

namespace Citas.Models;

/// <summary>
/// Identidad global de una persona (contexto Acceso); raíz de agregado. La tabla fue
/// creada manualmente con nombres en minúscula (personas, id, nombre, ...), por eso se
/// mapea explícitamente en OnModelCreating. NO pertenece a un tenant (no tiene TenantId).
/// </summary>
public class Persona
{
    // Constructor sin parámetros para EF Core.
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

    /// <summary>Hash de la contraseña para el login global del cliente (PBKDF2).</summary>
    public string? ClaveHash { get; private set; }

    public ICollection<Usuario> Usuarios { get; private set; } = [];

    /// <summary>
    /// Registra una nueva persona con credenciales para el login global del cliente.
    /// El <paramref name="claveHash"/> ya debe venir hasheado (responsabilidad de la app).
    /// </summary>
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

    /// <summary>Nombre completo para mostrar.</summary>
    public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
}
