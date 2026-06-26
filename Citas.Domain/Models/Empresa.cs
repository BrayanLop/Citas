using Citas.Domain.Common;

namespace Citas.Models;

// Empresa = tenant. Su Id es el código que termina como TenantId en el resto de tablas
// y que viaja en el claim 'tenant' del JWT.
public class Empresa
{
    private Empresa() { }

    public string Id { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public bool Activo { get; private set; } = true;

    public static Empresa Crear(string id, string nombre)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainException("El código de la empresa es obligatorio.");
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre de la empresa es obligatorio.");

        return new Empresa { Id = id.Trim(), Nombre = nombre.Trim(), Activo = true };
    }

    public static Empresa Rehidratar(string id, string nombre, bool activo)
        => new() { Id = id, Nombre = nombre, Activo = activo };

    public void Activar() => Activo = true;
    public void Desactivar() => Activo = false;

    public void Renombrar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre de la empresa es obligatorio.");
        Nombre = nombre.Trim();
    }
}
