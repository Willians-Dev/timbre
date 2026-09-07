namespace Timbre.Api.DTOs.Jornadas;

public class ActualizarJornadaLaboralDto
{
    public string Nombre { get; set; } = string.Empty;

    public TimeOnly HoraEntrada { get; set; }

    public TimeOnly HoraInicioAlmuerzo { get; set; }

    public TimeOnly HoraFinAlmuerzo { get; set; }

    public TimeOnly HoraSalida { get; set; }

    public int ToleranciaEntradaMinutos { get; set; }

    public bool Lunes { get; set; }

    public bool Martes { get; set; }

    public bool Miercoles { get; set; }

    public bool Jueves { get; set; }

    public bool Viernes { get; set; }

    public bool Sabado { get; set; }

    public bool Domingo { get; set; }

    public bool Activo { get; set; }
}