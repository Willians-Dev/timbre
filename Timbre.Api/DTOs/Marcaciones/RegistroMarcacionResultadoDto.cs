namespace Timbre.Api.DTOs.Marcaciones;

public class RegistroMarcacionResultadoDto
{
    public long IdMarcacion { get; set; }

    public long IdEmpleado { get; set; }

    public string Empleado { get; set; } =
        string.Empty;

    public DateOnly Fecha { get; set; }

    public DateTime FechaHora { get; set; }

    public string TipoMarcacion { get; set; } =
        string.Empty;

    public EstadoEntradaDto? EstadoEntrada { get; set; }

    public bool Completa { get; set; }

    public List<string> MarcacionesFaltantes { get; set; } =
        [];

    public string Mensaje { get; set; } =
        string.Empty;
}