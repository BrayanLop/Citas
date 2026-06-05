using System.Reflection;
using Citas.Models;
using Citas.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace Citas.Data;

public class CitasContext : DbContext
{
    private readonly string? _tenantId;

    public CitasContext(DbContextOptions<CitasContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantId = tenantProvider.GetTenantId();
    }

    /// <summary>Tenant actual; usado por los filtros globales de consulta.</summary>
    public string? CurrentTenantId => _tenantId;

    public DbSet<Empresa> Empresas { get; set; }
    public DbSet<Persona> Personas { get; set; }
    public DbSet<Rol> Roles { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<RolUsuario> RolUsuarios { get; set; }
    public DbSet<Servicio> Servicios { get; set; }
    public DbSet<Cita> Citas { get; set; }
    public DbSet<DetalleCita> DetalleCitas { get; set; }
    public DbSet<AgendaEmpleado> AgendaEmpleados { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Empresa (catalogo de tenants) - NO se filtra por tenant.
        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.HasData(new Empresa { Id = "DEFAULT", Nombre = "Empresa por defecto", Activo = true });
        });

        // Rol
        modelBuilder.Entity<Rol>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NombreRol).IsRequired().HasMaxLength(100);
        });

        // Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NombreUsuario).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Estado).HasDefaultValue(true);
            entity.HasOne(e => e.Persona)
                .WithMany(p => p.Usuarios)
                .HasForeignKey(e => e.PersonaId);
        });

        // Persona (tabla creada manualmente con nombres en minuscula; no es tenant).
        modelBuilder.Entity<Persona>(entity =>
        {
            entity.ToTable("personas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nombre).HasColumnName("nombre").IsRequired();
            entity.Property(e => e.Apellido).HasColumnName("apellido").IsRequired();
            entity.Property(e => e.Cedula).HasColumnName("cedula");
            entity.Property(e => e.Residencia).HasColumnName("residencia");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Edad).HasColumnName("edad");
            entity.Property(e => e.Celular).HasColumnName("celular");
            entity.Property(e => e.FechaNacimiento).HasColumnName("fechanacimiento");
        });

        // RolUsuario
        modelBuilder.Entity<RolUsuario>(entity =>
        {
            entity.HasKey(e => e.IdRolUsuario);
            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.RolUsuarios)
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Rol)
                .WithMany(r => r.RolUsuarios)
                .HasForeignKey(e => e.IdRol)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Servicio
        modelBuilder.Entity<Servicio>(entity =>
        {
            entity.HasKey(e => e.IdServicio);
            entity.Property(e => e.NombreServicio).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Valor).HasPrecision(18, 2);
        });

        // Cita
        modelBuilder.Entity<Cita>(entity =>
        {
            entity.HasKey(e => e.IdCita);
            entity.Property(e => e.Estado).HasMaxLength(50);
            entity.HasOne(e => e.Cliente)
                .WithMany(u => u.CitasCliente)
                .HasForeignKey(e => e.IdCliente)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Empleado)
                .WithMany(u => u.CitasEmpleado)
                .HasForeignKey(e => e.IdEmpleado)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // DetalleCita
        modelBuilder.Entity<DetalleCita>(entity =>
        {
            entity.HasKey(e => e.IdDetalle);
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

        // AgendaEmpleado
        modelBuilder.Entity<AgendaEmpleado>(entity =>
        {
            entity.HasKey(e => e.IdHorario);
            entity.Property(e => e.EstadoHorario).HasMaxLength(50);
            entity.HasOne(e => e.Usuario)
                .WithMany(u => u.AgendaEmpleado)
                .HasForeignKey(e => e.IdUsuario)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== Configuracion comun de multitenancy =====
        // Para toda entidad que implemente ITenantEntity: columna TenantId (requerida,
        // indexada) y un filtro global que aisla los datos por tenant en cada consulta.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType, b =>
                {
                    b.Property(nameof(ITenantEntity.TenantId)).HasMaxLength(50).IsRequired();
                    b.HasIndex(nameof(ITenantEntity.TenantId));
                });

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

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantEntity
    {
        // EF re-evalua CurrentTenantId como parametro en cada consulta.
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

    /// <summary>Sella el TenantId en altas y evita que se modifique en actualizaciones.</summary>
    private void ApplyTenantToChanges()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
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
                    // El tenant de un registro existente no se puede cambiar.
                    entry.Property(nameof(ITenantEntity.TenantId)).IsModified = false;
                    break;
            }
        }
    }
}
