namespace Timbre.Api.DTOs.Empleados;

public class EmpleadoDto
{
    public long IdEmpleado { get; set; }

    public string Identificacion { get; set; } = string.Empty;

    public string Nombres { get; set; } = string.Empty;

    public string Apellidos { get; set; } = string.Empty;

    public string NombreCompleto => $"{Nombres} {Apellidos}";

    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public string? Area { get; set; }

    public string? Cargo { get; set; }

    public DateOnly? FechaIngreso { get; set; }

    public DateOnly? FechaSalida { get; set; }

    public long IdJornada { get; set; }

    public string Jornada { get; set; } = string.Empty;

    public bool Activo { get; set; }
}