using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Timbre.Api.Models;

namespace Timbre.Api.Services;

public class JwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(
        IConfiguration configuration)
    {
        _configuration = configuration;
    }


    public (
        string Token,
        DateTime Expira
    ) GenerarToken(
        Usuario usuario)
    {
        // =================================================
        // VALIDACIONES
        // =================================================
        if (usuario is null)
        {
            throw new ArgumentNullException(
                nameof(usuario)
            );
        }


        if (
            usuario.IdRolNavigation is null
        )
        {
            throw new InvalidOperationException(
                "El usuario no tiene un rol cargado."
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                usuario.IdRolNavigation.Nombre
            )
        )
        {
            throw new InvalidOperationException(
                "El rol del usuario no tiene un nombre válido."
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                usuario.NombreUsuario
            )
        )
        {
            throw new InvalidOperationException(
                "El usuario no tiene un nombre válido."
            );
        }


        // =================================================
        // CONFIGURACIÓN JWT
        // =================================================
        var jwt =
            _configuration
                .GetSection(
                    "Jwt"
                );


        var key =
            jwt["Key"];

        if (
            string.IsNullOrWhiteSpace(
                key
            )
        )
        {
            throw new InvalidOperationException(
                "No se encontró Jwt:Key."
            );
        }


        /*
         * Para HMAC-SHA256 conviene usar una llave
         * suficientemente larga.
         */
        if (
            Encoding.UTF8
                .GetByteCount(key) <
            32
        )
        {
            throw new InvalidOperationException(
                "Jwt:Key debe tener al menos 32 bytes para HMAC-SHA256."
            );
        }


        var issuer =
            jwt["Issuer"];


        var audience =
            jwt["Audience"];


        if (
            string.IsNullOrWhiteSpace(
                issuer
            )
        )
        {
            throw new InvalidOperationException(
                "No se encontró Jwt:Issuer."
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                audience
            )
        )
        {
            throw new InvalidOperationException(
                "No se encontró Jwt:Audience."
            );
        }


        if (
            !int.TryParse(
                jwt[
                    "ExpirationMinutes"
                ],
                out var expirationMinutes
            )
        )
        {
            expirationMinutes =
                60;
        }


        if (
            expirationMinutes <= 0
        )
        {
            expirationMinutes =
                60;
        }


        // =================================================
        // EXPIRACIÓN
        // =================================================
        var expira =
            DateTime.UtcNow
                .AddMinutes(
                    expirationMinutes
                );


        // =================================================
        // CLAIMS
        // =================================================
        var claims =
            new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    usuario.IdUsuario
                        .ToString()
                ),

                new(
                    ClaimTypes.Name,
                    usuario.NombreUsuario
                ),

                new(
                    ClaimTypes.Role,
                    usuario
                        .IdRolNavigation
                        .Nombre
                ),

                new(
                    "idEmpleado",
                    usuario.IdEmpleado
                        .ToString()
                ),

                new(
                    "idRol",
                    usuario.IdRol
                        .ToString()
                )
            };


        // =================================================
        // FIRMA
        // =================================================
        var signingKey =
            new SymmetricSecurityKey(
                Encoding.UTF8
                    .GetBytes(
                        key
                    )
            );


        var credentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256
            );


        // =================================================
        // TOKEN
        // =================================================
        var token =
            new JwtSecurityToken(
                issuer:
                    issuer,

                audience:
                    audience,

                claims:
                    claims,

                expires:
                    expira,

                signingCredentials:
                    credentials
            );


        var tokenString =
            new JwtSecurityTokenHandler()
                .WriteToken(
                    token
                );


        return (
            tokenString,
            expira
        );
    }
}