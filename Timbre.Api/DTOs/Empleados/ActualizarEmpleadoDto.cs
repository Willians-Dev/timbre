namespace Timbre.Api.DTOs.Empleados;

public class ActualizarEmpleadoDto
{
    public string Identificacion { get; set; } = string.Empty;

    public string Nombres { get; set; } = string.Empty;

    public string Apellidos { get; set; } = string.Empty;

    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public string? Area { get; set; }

    public string? Cargo { get; set; }

    public DateOnly? FechaIngreso { get; set; }

    public DateOnly? FechaSalida { get; set; }

    public long IdJornada { get; set; }

    public bool Activo { get; set; }
}