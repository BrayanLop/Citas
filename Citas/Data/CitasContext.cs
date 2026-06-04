using Citas.Models;
using Microsoft.EntityFrameworkCore;

namespace Citas.Data;

public class CitasContext : DbContext
{
    public CitasContext(DbContextOptions<CitasContext> options) : base(options)
    {
    }

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
    }
}
