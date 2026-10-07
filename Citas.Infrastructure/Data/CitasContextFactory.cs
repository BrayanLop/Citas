using Citas.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Citas.Data;

/// <summary>
/// Fábrica usada solo en tiempo de diseño por las herramientas de EF Core
/// (dotnet ef migrations/database). No participa en la ejecución de la app.
/// La cadena de conexión se toma de la variable de entorno
/// ConnectionStrings__DefaultConnection o del appsettings(.Development).json del
/// proyecto de arranque; no hay valores de respaldo.
/// </summary>
public class CitasContextFactory : IDesignTimeDbContextFactory<CitasContext>
{
    public CitasContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connection = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                "Falta la cadena de conexión de Citas. Define la variable de entorno " +
                "ConnectionStrings__DefaultConnection (o ConnectionStrings:DefaultConnection " +
                "en Citas/appsettings.json) antes de usar dotnet ef.");

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
