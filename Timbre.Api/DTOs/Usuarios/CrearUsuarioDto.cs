namespace Timbre.Api.DTOs.Usuarios;

public class CrearUsuarioDto
{
    public long IdEmpleado { get; set; }

    public long IdRol { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}