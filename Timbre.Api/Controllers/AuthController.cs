using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Timbre.Api.Data;
using Timbre.Api.DTOs.Auth;
using Timbre.Api.Services;


namespace Timbre.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TimbreDbContext
        _context;

    private readonly PasswordService
        _passwordService;

    private readonly JwtService
        _jwtService;

    private readonly UsernameService
        _usernameService;

    private readonly ILogger<AuthController>
        _logger;


    public AuthController(
        TimbreDbContext context,
        PasswordService passwordService,
        JwtService jwtService,
        UsernameService usernameService,
        ILogger<AuthController> logger)
    {
        _context =
            context;

        _passwordService =
            passwordService;

        _jwtService =
            jwtService;

        _usernameService =
            usernameService;

        _logger =
            logger;
    }


    // =====================================================
    // POST: api/auth/login
    // =====================================================

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>>
        Login(
            [FromBody] LoginDto dto,
            CancellationToken cancellationToken)
    {
        // =================================================
        // VALIDAR REQUEST
        // =================================================

        if (dto is null)
        {
            return BadRequest(new
            {
                mensaje =
                    "La solicitud de autenticación es inválida."
            });
        }


        if (
            string.IsNullOrWhiteSpace(
                dto.NombreUsuario
            ) ||
            string.IsNullOrWhiteSpace(
                dto.Password
            )
        )
        {
            return BadRequest(new
            {
                mensaje =
                    "Usuario y contraseña son obligatorios."
            });
        }


        // =================================================
        // NORMALIZAR USUARIO
        // =================================================

        string nombreUsuario;

        try
        {
            nombreUsuario =
                _usernameService
                    .Normalizar(
                        dto.NombreUsuario
                    );
        }
        catch (ArgumentException)
        {
            // No exponemos reglas internas de validación
            // durante autenticación.
            return Unauthorized(new
            {
                mensaje =
                    "Usuario o contraseña incorrectos."
            });
        }


        // =================================================
        // CONSULTAR USUARIO
        // =================================================

        var usuario =
            await _context.Usuario
                .Include(u =>
                    u.IdEmpleadoNavigation)
                .Include(u =>
                    u.IdRolNavigation)
                .FirstOrDefaultAsync(
                    u =>
                        u.NombreUsuario ==
                        nombreUsuario,
                    cancellationToken
                );


        // =================================================
        // USUARIO NO EXISTE
        // =================================================

        if (usuario is null)
        {
            return Unauthorized(new
            {
                mensaje =
                    "Usuario o contraseña incorrectos."
            });
        }


        // =================================================
        // VALIDAR USUARIO
        // =================================================

        if (!usuario.Activo)
        {
            return Unauthorized(new
            {
                mensaje =
                    "El usuario se encuentra inactivo."
            });
        }


        // =================================================
        // VALIDAR RELACIÓN EMPLEADO
        // =================================================

        if (
            usuario.IdEmpleadoNavigation is null
        )
        {
            _logger.LogError(
                "El usuario {IdUsuario} no tiene empleado asociado.",
                usuario.IdUsuario
            );

            return StatusCode(
                StatusCodes
                    .Status500InternalServerError,

                new
                {
                    mensaje =
                        "El usuario no tiene un empleado asociado correctamente."
                }
            );
        }


        if (
            !usuario
                .IdEmpleadoNavigation
                .Activo
        )
        {
            return Unauthorized(new
            {
                mensaje =
                    "El empleado se encuentra inactivo."
            });
        }


        // =================================================
        // VALIDAR RELACIÓN ROL
        // =================================================

        if (
            usuario.IdRolNavigation is null
        )
        {
            _logger.LogError(
                "El usuario {IdUsuario} no tiene rol asociado.",
                usuario.IdUsuario
            );

            return StatusCode(
                StatusCodes
                    .Status500InternalServerError,

                new
                {
                    mensaje =
                        "El usuario no tiene un rol asociado correctamente."
                }
            );
        }


        if (
            !usuario
                .IdRolNavigation
                .Activo
        )
        {
            return Unauthorized(new
            {
                mensaje =
                    "El rol se encuentra inactivo."
            });
        }


        // =================================================
        // VALIDAR HASH
        // =================================================

        if (
            string.IsNullOrWhiteSpace(
                usuario.PasswordHash
            )
        )
        {
            _logger.LogError(
                "El usuario {IdUsuario} no tiene PasswordHash configurado.",
                usuario.IdUsuario
            );

            return StatusCode(
                StatusCodes
                    .Status500InternalServerError,

                new
                {
                    mensaje =
                        "El usuario no tiene una contraseña configurada correctamente."
                }
            );
        }


        // =================================================
        // VERIFICAR CONTRASEÑA
        // =================================================

        bool passwordValido;

        try
        {
            passwordValido =
                _passwordService
                    .VerificarPassword(
                        usuario.PasswordHash,
                        dto.Password
                    );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error verificando contraseña del usuario {IdUsuario}.",
                usuario.IdUsuario
            );

            return StatusCode(
                StatusCodes
                    .Status500InternalServerError,

                new
                {
                    mensaje =
                        "No fue posible validar las credenciales."
                }
            );
        }


        if (!passwordValido)
        {
            return Unauthorized(new
            {
                mensaje =
                    "Usuario o contraseña incorrectos."
            });
        }


        // =================================================
        // GENERAR JWT
        // =================================================

        (string Token, DateTime Expira) resultado;

        try
        {
            resultado =
                _jwtService
                    .GenerarToken(
                        usuario
                    );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error generando JWT para usuario {IdUsuario}.",
                usuario.IdUsuario
            );

            return StatusCode(
                StatusCodes
                    .Status500InternalServerError,

                new
                {
                    mensaje =
                        "No fue posible generar el token de autenticación."
                }
            );
        }


        // =================================================
        // ÚLTIMO ACCESO
        // =================================================

        usuario.UltimoAcceso =
            DateTime.Now;


        await _context
            .SaveChangesAsync(
                cancellationToken
            );


        // =================================================
        // LOG DE ACCESO
        // =================================================

        _logger.LogInformation(
            "Inicio de sesión correcto para usuario {IdUsuario}.",
            usuario.IdUsuario
        );


        // =================================================
        // RESPUESTA
        // =================================================

        return Ok(
            new LoginResponseDto
            {
                Token =
                    resultado.Token,

                Expira =
                    resultado.Expira,

                IdUsuario =
                    usuario.IdUsuario,

                NombreUsuario =
                    usuario.NombreUsuario,

                IdEmpleado =
                    usuario.IdEmpleado,

                Empleado =
                    usuario
                        .IdEmpleadoNavigation
                        .Nombres +
                    " " +
                    usuario
                        .IdEmpleadoNavigation
                        .Apellidos,

                IdRol =
                    usuario.IdRol,

                Rol =
                    usuario
                        .IdRolNavigation
                        .Nombre
            }
        );
    }
}