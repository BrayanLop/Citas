namespace Citas.Application.Sincronizacion;

/// <summary>Cuántos registros creó, actualizó y dejó inactivos la sincronización.</summary>
public class ResumenCambiosDto
{
    public int Creados { get; set; }
    public int Actualizados { get; set; }
    public int Desactivados { get; set; }
}

/// <summary>Respuesta de POST /Sincronizacion/Natural.</summary>
public class ResumenSincronizacionDto
{
    public ResumenCambiosDto Servicios { get; set; } = new();
    public ResumenCambiosDto Personal { get; set; } = new();
}
