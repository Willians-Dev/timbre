namespace Timbre.Api.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;

    public DateTime Expira { get; set; }

    public long IdUsuario { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;

    public long IdEmpleado { get; set; }

    public string Empleado { get; set; } = string.Empty;

    public long IdRol { get; set; }

    public string Rol { get; set; } = string.Empty;
}