using Citas.Domain.Common;

namespace Citas.Models;

/// <summary>
/// Franja de trabajo de un día de la semana, en hora local del negocio
/// (p. ej. lunes 08:00-12:00). Inicio incluido, fin excluido.
/// </summary>
public sealed record FranjaHoraria(DayOfWeek Dia, TimeOnly Inicio, TimeOnly Fin)
{
    public bool SeCruzaCon(FranjaHoraria otra)
        => Dia == otra.Dia && Inicio < otra.Fin && otra.Inicio < Fin;

    /// <summary>El tramo [inicio, fin) del mismo día cabe completo en la franja.</summary>
    public bool Contiene(TimeOnly inicio, TimeOnly fin) => Inicio <= inicio && fin <= Fin;
}

// Horario semanal de un empleado: varias franjas por día (p. ej. mañana y tarde con la
// pausa del almuerzo en medio), sin solapes entre franjas del mismo día. Un día sin
// franjas es un día en que no trabaja. Se reemplaza completo (Definir), no por partes.
public class AgendaEmpleado
{
    private AgendaEmpleado() { }

    public int IdEmpleado { get; private set; }

    private List<FranjaHoraria> _franjas = [];
    public IReadOnlyList<FranjaHoraria> Franjas => _franjas;

    public static AgendaEmpleado Definir(int idEmpleado, IEnumerable<FranjaHoraria> franjas)
    {
        if (idEmpleado <= 0)
            throw new DomainException("El horario requiere un empleado válido.");

        var lista = Ordenar(franjas);
        foreach (var f in lista)
        {
            if (!Enum.IsDefined(f.Dia))
                throw new DomainException($"Día de la semana no válido: {(int)f.Dia} (0 = domingo ... 6 = sábado).");
            if (f.Fin <= f.Inicio)
                throw new DomainException($"En la franja del {NombreDia(f.Dia)} {f.Inicio:HH\\:mm}-{f.Fin:HH\\:mm} el inicio debe ser anterior al fin.");
        }

        for (var i = 1; i < lista.Count; i++)
        {
            if (lista[i].SeCruzaCon(lista[i - 1]))
                throw new DomainException(
                    $"Las franjas del {NombreDia(lista[i].Dia)} se solapan: " +
                    $"{lista[i - 1].Inicio:HH\\:mm}-{lista[i - 1].Fin:HH\\:mm} y {lista[i].Inicio:HH\\:mm}-{lista[i].Fin:HH\\:mm}.");
        }

        return new AgendaEmpleado { IdEmpleado = idEmpleado, _franjas = lista };
    }

    public static AgendaEmpleado Rehidratar(int idEmpleado, IEnumerable<FranjaHoraria> franjas)
        => new() { IdEmpleado = idEmpleado, _franjas = Ordenar(franjas) };

    public IEnumerable<FranjaHoraria> FranjasDel(DayOfWeek dia) => _franjas.Where(f => f.Dia == dia);

    /// <summary>El tramo [inicio, fin) de ese día cae completo dentro de una sola franja.</summary>
    public bool Cubre(DayOfWeek dia, TimeOnly inicio, TimeOnly fin)
        => fin > inicio && FranjasDel(dia).Any(f => f.Contiene(inicio, fin));

    public static string NombreDia(DayOfWeek dia) => dia switch
    {
        DayOfWeek.Monday => "lunes",
        DayOfWeek.Tuesday => "martes",
        DayOfWeek.Wednesday => "miércoles",
        DayOfWeek.Thursday => "jueves",
        DayOfWeek.Friday => "viernes",
        DayOfWeek.Saturday => "sábado",
        DayOfWeek.Sunday => "domingo",
        _ => dia.ToString(),
    };

    private static List<FranjaHoraria> Ordenar(IEnumerable<FranjaHoraria> franjas)
        => franjas.OrderBy(f => f.Dia).ThenBy(f => f.Inicio).ToList();
}
