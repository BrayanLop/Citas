using System.Reflection;
using Citas.Infrastructure.Persistencia;
using Citas.Infrastructure.Persistencia.Modelos;
using Citas.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace Citas.Data;

// DbContext de la app. Trabaja con los modelos de persistencia (*DbModel), nunca con
// las entidades de dominio: la traducción la hacen los mappers en cada repositorio.
public class CitasContext : DbContext
{
    private readonly string? _tenantId;

    public CitasContext(DbContextOptions<CitasContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantId = tenantProvider.GetTenantId();
    }

    // Tenant actual; lo usan los filtros globales de consulta.
    public string? CurrentTenantId => _tenantId;

    public DbSet<EmpresaDbModel> Empresas => Set<EmpresaDbModel>();
    public DbSet<PersonaDbModel> Personas => Set<PersonaDbModel>();
    public DbSet<RolDbModel> Roles => Set<RolDbModel>();
    public DbSet<UsuarioDbModel> Usuarios => Set<UsuarioDbModel>();
    public DbSet<RolUsuarioDbModel> RolUsuarios => Set<RolUsuarioDbModel>();
    public DbSet<ServicioDbModel> Servicios => Set<ServicioDbModel>();
    public DbSet<CitaDbModel> Citas => Set<CitaDbModel>();
    public DbSet<DetalleCitaDbModel> DetalleCitas => Set<DetalleCitaDbModel>();
    public DbSet<AgendaEmpleadoDbModel> AgendaEmpleados => Set<AgendaEmpleadoDbModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tablas, claves y columnas viven como anotaciones en los *DbModel.
        // Aquí queda lo que las anotaciones no expresan bien: relaciones, precisión,
        // defaults, el seed y la configuración común de multitenancy.

        modelBuilder.Entity<EmpresaDbModel>()
            .HasData(new EmpresaDbModel { Id = "DEFAULT", Nombre = "Empresa por defecto", Activo = true });

        modelBuilder.Entity<UsuarioDbModel>(entity =>
        {
            entity.Property(e => e.Estado).HasDefaultValue(true);
            entity.HasOne(e => e.Persona)
                .WithMany(p => p.Usuarios)
                .HasForeignKey(e => e.PersonaId);
        });

        modelBuilder.Entity<RolUsuarioDbModel>(entity =>
        {
            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.RolUsuarios)
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Rol)
                .WithMany(r => r.RolUsuarios)
                .HasForeignKey(e => e.IdRol)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ServicioDbModel>()
            .Property(e => e.Valor).HasPrecision(18, 2);

        modelBuilder.Entity<CitaDbModel>(entity =>
        {
            entity.HasOne(e => e.Cliente)
                .WithMany(u => u.CitasCliente)
                .HasForeignKey(e => e.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Empleado)
                .WithMany(u => u.CitasEmpleado)
                .HasForeignKey(e => e.IdEmpleado)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DetalleCitaDbModel>(entity =>
        {
            entity.Property(e => e.ValorServicio).HasPrecision(18, 2);
            entity.HasOne(e => e.Cita)
                .WithMany(c => c.DetalleCitas)
                .HasForeignKey(e => e.IdCita)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Servicio)
                .WithMany(s => s.DetalleCitas)
                .HasForeignKey(e => e.IdServicio)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AgendaEmpleadoDbModel>()
            .HasOne(e => e.Usuario)
            .WithMany(u => u.AgendaEmpleado)
            .HasForeignKey(e => e.IdUsuario)
            .OnDelete(DeleteBehavior.Cascade);

        // Multitenancy: a todo modelo que sea ITenantDbModel le ponemos índice por TenantId
        // y un filtro global que solo deja ver los registros del tenant actual.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantDbModel).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasIndex(nameof(ITenantDbModel.TenantId));
                ApplyTenantQueryFilter(modelBuilder, entityType.ClrType);
            }
        }
    }

    private void ApplyTenantQueryFilter(ModelBuilder modelBuilder, Type clrType)
    {
        typeof(CitasContext)
            .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(clrType)
            .Invoke(this, [modelBuilder]);
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantDbModel
    {
        // EF vuelve a leer CurrentTenantId como parámetro en cada consulta.
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
    }

    public override int SaveChanges()
    {
        ApplyTenantToChanges();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantToChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // Sella el TenantId en los registros nuevos y evita que se cambie en los existentes.
    private void ApplyTenantToChanges()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantDbModel>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (string.IsNullOrEmpty(entry.Entity.TenantId))
                    {
                        entry.Entity.TenantId = _tenantId
                            ?? throw new InvalidOperationException(
                                "No hay un tenant en el contexto. Falta el claim 'tenant' en el token.");
                    }
                    break;

                case EntityState.Modified:
                    // El tenant de un registro que ya existe no se toca.
                    entry.Property(nameof(ITenantDbModel.TenantId)).IsModified = false;
                    break;
            }
        }
    }
}
