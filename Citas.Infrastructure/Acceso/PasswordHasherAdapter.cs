using Citas.Application.Acceso.Abstractions;
using Citas.Helpers;

namespace Citas.Infrastructure.Acceso;

/// <summary>Adaptador del puerto <see cref="IPasswordHasher"/> sobre PBKDF2.</summary>
public class PasswordHasherAdapter : IPasswordHasher
{
    public string Hash(string password) => PasswordHasher.Hash(password);

    public bool Verify(string password, string? stored) => PasswordHasher.Verify(password, stored);
}
