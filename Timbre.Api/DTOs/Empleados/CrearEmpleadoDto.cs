namespace Timbre.Api.DTOs.Empleados;

public class CrearEmpleadoDto
{
    public string Identificacion { get; set; } = string.Empty;

    public string Nombres { get; set; } = string.Empty;

    public string Apellidos { get; set; } = string.Empty;

    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public string? Area { get; set; }

    public string? Cargo { get; set; }

    public DateOnly? FechaIngreso { get; set; }

    public long IdJornada { get; set; }
}