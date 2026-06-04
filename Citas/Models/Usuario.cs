namespace Citas.Models;

public class Usuario
{
    public int Id { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;

    public ICollection<RolUsuario> RolUsuarios { get; set; } = [];
    public ICollection<Cita> CitasCliente { get; set; } = [];
    public ICollection<Cita> CitasEmpleado { get; set; } = [];
    public ICollection<AgendaEmpleado> AgendaEmpleado { get; set; } = [];
}
