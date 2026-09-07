namespace Timbre.Api.DTOs.Usuarios;

public class ActualizarUsuarioDto
{
    public long IdRol { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;

    public bool Activo { get; set; }
}