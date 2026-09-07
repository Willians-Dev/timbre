namespace Timbre.Api.DTOs.Marcaciones;

public class EstadoEntradaDto
{
    public string Estado { get; set; } = string.Empty;

    public int MinutosAtraso { get; set; }

    public TimeOnly HoraEntradaProgramada { get; set; }

    public int ToleranciaMinutos { get; set; }

    public TimeOnly HoraLimitePuntual { get; set; }

    public TimeOnly HoraMarcacion { get; set; }
}