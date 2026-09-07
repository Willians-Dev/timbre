namespace Timbre.Api.DTOs.Marcaciones;

public class MarcacionDto
{
    public long IdMarcacion { get; set; }

    public long IdEmpleado { get; set; }

    public string Empleado { get; set; } = string.Empty;

    public DateOnly FechaMarcacion { get; set; }

    public DateTime FechaHora { get; set; }

    public string TipoMarcacion { get; set; } = string.Empty;

    public string EstadoMarcacion { get; set; } = string.Empty;

    public string? Observacion { get; set; }

    public string? EstadoEntrada { get; set; }

    public int? MinutosAtraso { get; set; }
}