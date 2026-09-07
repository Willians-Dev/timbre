namespace Timbre.Api.DTOs.Asistencia;

public class ResumenHistoricoAsistenciaDto
{
    public DateOnly Fecha { get; set; }

    public long IdEmpleado { get; set; }

    public string Empleado { get; set; } = string.Empty;

    public string? Area { get; set; }

    public string? Cargo { get; set; }

    public string Jornada { get; set; } = string.Empty;

    public TimeOnly? Entrada { get; set; }

    public TimeOnly? InicioAlmuerzo { get; set; }

    public TimeOnly? FinAlmuerzo { get; set; }

    public TimeOnly? Salida { get; set; }

    public string? EstadoEntrada { get; set; }

    public int MinutosAtraso { get; set; }

    public int? MinutosAlmuerzo { get; set; }

    public bool SalidaAnticipada { get; set; }

    public int MinutosSalidaAnticipada { get; set; }

    public int? MinutosPermanencia { get; set; }

    public int? MinutosTrabajados { get; set; }

    public string EstadoJornada { get; set; } = string.Empty;

    public bool Completa { get; set; }

    public List<string> MarcacionesFaltantes { get; set; } = new();
}