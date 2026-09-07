using Microsoft.AspNetCore.Identity;

namespace Timbre.Api.Services;

public class PasswordService
{
    private readonly PasswordHasher<object> _passwordHasher = new();

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(new object(), password);
    }

    public bool VerificarPassword(
        string passwordHash,
        string password)
    {
        var resultado = _passwordHasher.VerifyHashedPassword(
            new object(),
            passwordHash,
            password);

        return resultado == PasswordVerificationResult.Success ||
               resultado == PasswordVerificationResult.SuccessRehashNeeded;
    }
}