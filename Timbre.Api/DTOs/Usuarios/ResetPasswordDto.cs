namespace Timbre.Api.DTOs.Usuarios;

public class ResetPasswordDto
{
    public string PasswordNuevo { get; set; } =
        string.Empty;

    public string ConfirmarPassword { get; set; } =
        string.Empty;
}