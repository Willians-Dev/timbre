namespace Timbre.Api.DTOs.Historial;


// =========================================================
// FILTRO
// =========================================================

public class HistorialAsistenciaFiltroDto
{
    public DateOnly FechaDesde { get; set; }

    public DateOnly FechaHasta { get; set; }

    public long? IdEmpleado { get; set; }

    public string? Area { get; set; }

    public string? EstadoJornada { get; set; }

    public string? EstadoEntrada { get; set; }

    public string? Origen { get; set; }
}


// =========================================================
// MARCACIÓN INDIVIDUAL
// =========================================================

public class HistorialMarcacionDto
{
    public long IdMarcacion { get; set; }

    public string Hora { get; set; } =
        string.Empty;

    public string TipoMarcacion { get; set; } =
        string.Empty;

    public string EstadoMarcacion { get; set; } =
        string.Empty;

    public string? Observacion { get; set; }

    public bool EsFacial { get; set; }
}


// =========================================================
// JORNADA CONSOLIDADA
// =========================================================

public class HistorialRegistroDto
{
    public long IdEmpleado { get; set; }

    public string Identificacion { get; set; } =
        string.Empty;

    public string Empleado { get; set; } =
        string.Empty;

    public string? Area { get; set; }

    public string? Cargo { get; set; }

    public DateOnly Fecha { get; set; }

    public string? Entrada { get; set; }

    public string? InicioAlmuerzo { get; set; }

    public string? FinAlmuerzo { get; set; }

    public string? Salida { get; set; }

    public string? EstadoEntrada { get; set; }

    public int MinutosAtraso { get; set; }

    public bool Completa { get; set; }

    public string EstadoJornada { get; set; } =
        string.Empty;

    public bool TieneFacial { get; set; }

    public bool TieneManual { get; set; }

    public string Origen { get; set; } =
        string.Empty;

    public List<HistorialMarcacionDto>
        Marcaciones { get; set; } =
            new();
}


// =========================================================
// RESPUESTA
// =========================================================

public class HistorialAsistenciaResultadoDto
{
    public DateOnly FechaDesde { get; set; }

    public DateOnly FechaHasta { get; set; }

    public int Total { get; set; }

    public List<HistorialRegistroDto>
        Registros { get; set; } =
            new();
}


// =========================================================
// FILTROS FRONTEND
// =========================================================

public class HistorialEmpleadoFiltroDto
{
    public long IdEmpleado { get; set; }

    public string Nombre { get; set; } =
        string.Empty;

    public string? Area { get; set; }
}


public class HistorialFiltrosDto
{
    public List<HistorialEmpleadoFiltroDto>
        Empleados { get; set; } =
            new();

    public List<string>
        Areas { get; set; } =
            new();
}