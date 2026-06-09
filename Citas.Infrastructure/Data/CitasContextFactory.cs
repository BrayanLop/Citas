using Citas.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Citas.Data;

/// <summary>
/// Fábrica usada solo en tiempo de diseño por las herramientas de EF Core
/// (dotnet ef migrations/database). No participa en la ejecución de la app.
/// </summary>
public class CitasContextFactory : IDesignTimeDbContextFactory<CitasContext>
{
    public CitasContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=citas;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<CitasContext>()
            .UseNpgsql(connection)
            .Options;

        return new CitasContext(options, new NullTenantProvider());
    }

    /// <summary>En diseño no hay petición HTTP; el tenant no aplica.</summary>
    private sealed class NullTenantProvider : ITenantProvider
    {
        public string? GetTenantId() => null;
    }
}
