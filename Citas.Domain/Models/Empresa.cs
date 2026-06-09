using Citas.Domain.Common;

namespace Citas.Models;

/// <summary>
/// Catálogo de empresas/tenants y raíz de agregado del contexto Empresas. El
/// <see cref="Id"/> es el código que se usa como TenantId en el resto de las tablas
/// y que viaja en el claim 'tenant' del JWT. Se crea con <see cref="Crear"/>.
/// </summary>
public class Empresa
{
    // Constructor sin parámetros para EF Core.
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

    public void Activar() => Activo = true;
    public void Desactivar() => Activo = false;

    public void Renombrar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DomainException("El nombre de la empresa es obligatorio.");
        Nombre = nombre.Trim();
    }
}
