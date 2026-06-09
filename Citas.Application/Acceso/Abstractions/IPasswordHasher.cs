namespace Citas.Application.Acceso.Abstractions;

/// <summary>
/// Puerto para el hashing y verificación de contraseñas. La implementación
/// (PBKDF2) vive en la infraestructura.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string? stored);
}
