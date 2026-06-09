namespace Citas.Models;

public class Usuario : ITenantEntity
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string? ClaveHash { get; set; }
    public bool? Estado { get; set; } = true;
    public int? PersonaId { get; set; }

    public Persona? Persona { get; set; }

    public ICollection<RolUsuario> RolUsuarios { get; set; } = [];
    public ICollection<Cita> CitasCliente { get; set; } = [];
    public ICollection<Cita> CitasEmpleado { get; set; } = [];
    public ICollection<AgendaEmpleado> AgendaEmpleado { get; set; } = [];
}
