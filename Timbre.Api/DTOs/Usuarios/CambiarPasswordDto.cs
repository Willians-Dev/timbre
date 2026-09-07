namespace Timbre.Api.DTOs.Usuarios;

public class CambiarPasswordDto
{
    public string PasswordActual { get; set; } =
        string.Empty;

    public string PasswordNuevo { get; set; } =
        string.Empty;

    public string ConfirmarPassword { get; set; } =
        string.Empty;
}