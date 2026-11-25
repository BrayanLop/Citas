using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Citas.Infrastructure.Persistencia.Modelos;

// Conexión de un empleado con Google Calendar. El refresh token va cifrado (AES-GCM).
[Table("CuentasGoogle")]
public class CuentaGoogleDbModel : ITenantDbModel
{
    [Key]
    public int IdCuenta { get; set; }

    public int IdEmpleado { get; set; }

    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string RefreshTokenCifrado { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Estado { get; set; } = "Activa";

    [Required]
    [MaxLength(200)]
    public string CalendarioId { get; set; } = "primary";

    [MaxLength(500)]
    public string? SyncToken { get; set; }

    public DateTime? UltimaSincronizacionUtc { get; set; }
    public bool CambioRemotoPendiente { get; set; }
    public int Intentos { get; set; }
    public DateTime? ProximoIntentoUtc { get; set; }

    [MaxLength(300)]
    public string? UltimoError { get; set; }

    [MaxLength(64)]
    public string? CanalId { get; set; }

    public DateTime? CanalExpiraUtc { get; set; }
    public DateTime ConectadaUtc { get; set; }

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    public UsuarioDbModel? Empleado { get; set; }
}

// Cita publicada en el calendario del empleado. Sin llave foránea a Citas a propósito: si la cita
// se borra, el vínculo queda como "huérfano" y el sincronizador elimina el evento de Google.
[Table("GoogleEventosCita")]
public class GoogleEventoCitaDbModel : ITenantDbModel
{
    [Key]
    public int Id { get; set; }

    public int IdCita { get; set; }
    public int IdEmpleado { get; set; }

    [Required]
    [MaxLength(200)]
    public string GoogleEventId { get; set; } = string.Empty;

    [Required]
    [MaxLength(64)]
    public string Huella { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;
}

// Bloqueo de agenda que nació de un evento ocupado del Google Calendar del empleado.
[Table("GoogleBloqueos")]
public class GoogleBloqueoDbModel : ITenantDbModel
{
    [Key]
    public int Id { get; set; }

    public int IdBloqueo { get; set; }
    public int IdEmpleado { get; set; }

    [Required]
    [MaxLength(200)]
    public string GoogleEventId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string TenantId { get; set; } = string.Empty;

    public BloqueoAgendaDbModel? Bloqueo { get; set; }
}
