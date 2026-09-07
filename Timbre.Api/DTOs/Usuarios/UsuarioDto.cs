namespace Timbre.Api.DTOs.Usuarios;

public class UsuarioDto
{
    public long IdUsuario { get; set; }

    public long IdEmpleado { get; set; }

    public string Empleado { get; set; } = string.Empty;

    public long IdRol { get; set; }

    public string Rol { get; set; } = string.Empty;

    public string NombreUsuario { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public DateTime? UltimoAcceso { get; set; }
}