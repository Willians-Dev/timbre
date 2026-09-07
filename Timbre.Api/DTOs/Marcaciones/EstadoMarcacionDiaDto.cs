namespace Timbre.Api.DTOs.Marcaciones;

public class EstadoMarcacionDiaDto
{
    public long IdEmpleado { get; set; }

    public string Empleado { get; set; } = string.Empty;

    public DateOnly Fecha { get; set; }

    public bool TieneEntrada { get; set; }

    public bool TieneInicioAlmuerzo { get; set; }

    public bool TieneFinAlmuerzo { get; set; }

    public bool TieneSalida { get; set; }

    public string? EstadoEntrada { get; set; }

    public int MinutosAtraso { get; set; }

    public bool Completa { get; set; }

    public List<string> MarcacionesFaltantes { get; set; } = new();
}