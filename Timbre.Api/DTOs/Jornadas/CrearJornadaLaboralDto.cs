namespace Timbre.Api.DTOs.Jornadas;

public class CrearJornadaLaboralDto
{
    public string Nombre { get; set; } = string.Empty;

    public TimeOnly HoraEntrada { get; set; }

    public TimeOnly HoraInicioAlmuerzo { get; set; }

    public TimeOnly HoraFinAlmuerzo { get; set; }

    public TimeOnly HoraSalida { get; set; }

    public int ToleranciaEntradaMinutos { get; set; }

    public bool Lunes { get; set; } = true;

    public bool Martes { get; set; } = true;

    public bool Miercoles { get; set; } = true;

    public bool Jueves { get; set; } = true;

    public bool Viernes { get; set; } = true;

    public bool Sabado { get; set; }

    public bool Domingo { get; set; }
}