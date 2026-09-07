namespace Timbre.Api.DTOs.Asistencia;

public class ResumenDiarioAsistenciaDto
{
    public long IdEmpleado { get; set; }

    public string Empleado { get; set; } = string.Empty;

    public string? Area { get; set; }

    public string? Cargo { get; set; }

    public DateOnly Fecha { get; set; }

    public long IdJornada { get; set; }

    public string Jornada { get; set; } = string.Empty;

    public TimeOnly HoraEntradaProgramada { get; set; }

    public TimeOnly HoraSalidaProgramada { get; set; }

    public int ToleranciaEntradaMinutos { get; set; }

    // Marcaciones
    public TimeOnly? Entrada { get; set; }

    public TimeOnly? InicioAlmuerzo { get; set; }

    public TimeOnly? FinAlmuerzo { get; set; }

    public TimeOnly? Salida { get; set; }

    // Puntualidad
    public string? EstadoEntrada { get; set; }

    public int MinutosAtraso { get; set; }

    // Almuerzo
    public int? MinutosAlmuerzo { get; set; }

    // Salida
    public bool SalidaAnticipada { get; set; }

    public int MinutosSalidaAnticipada { get; set; }

    // Tiempo
    public int? MinutosPermanencia { get; set; }

    public int? MinutosTrabajados { get; set; }

    // Estado general
    public string EstadoJornada { get; set; } = string.Empty;

    public bool Completa { get; set; }

    public List<string> MarcacionesFaltantes { get; set; } = new();
}