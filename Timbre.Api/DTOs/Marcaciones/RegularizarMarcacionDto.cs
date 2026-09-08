namespace Timbre.Api.DTOs.Marcaciones;


public class RegularizarMarcacionDto
{
    public long IdEmpleado { get; set; }


    public DateOnly Fecha { get; set; }


    public TimeOnly Hora { get; set; }


    public string TipoMarcacion { get; set; } =
        string.Empty;


    public string Motivo { get; set; } =
        string.Empty;
}