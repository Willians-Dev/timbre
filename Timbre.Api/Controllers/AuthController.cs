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
    private readonly TimbreDbContext _context;
    private readonly PasswordService _passwordService;
    private readonly JwtService _jwtService;

    public AuthController(
        TimbreDbContext context,
        PasswordService passwordService,
        JwtService jwtService)
    {
        _context = context;
        _passwordService = passwordService;
        _jwtService = jwtService;
    }


    // =====================================================
    // POST: api/auth/login
    // =====================================================
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
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
            string.IsNullOrWhiteSpace(dto.NombreUsuario) ||
            string.IsNullOrWhiteSpace(dto.Password)
        )
        {
            return BadRequest(new
            {
                mensaje =
                    "Usuario y contraseña son obligatorios."
            });
        }


        var nombreUsuario =
            dto.NombreUsuario
                .Trim()
                .ToLowerInvariant();


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
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    mensaje =
                        "El usuario no tiene un empleado asociado correctamente."
                }
            );
        }


        if (
            !usuario.IdEmpleadoNavigation.Activo
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
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    mensaje =
                        "El usuario no tiene un rol asociado correctamente."
                }
            );
        }


        if (
            !usuario.IdRolNavigation.Activo
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
            return StatusCode(
                StatusCodes.Status500InternalServerError,
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
            Console.WriteLine(
                $"Error verificando password del usuario {usuario.IdUsuario}: {ex}"
            );

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    mensaje =
                        "La contraseña almacenada del usuario no tiene un formato válido."
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
            Console.WriteLine(
                $"Error generando JWT para usuario {usuario.IdUsuario}: {ex}"
            );

            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    mensaje =
                        $"No fue posible generar el token de autenticación: {ex.Message}"
                }
            );
        }


        // =================================================
        // ÚLTIMO ACCESO
        // =================================================
        usuario.UltimoAcceso =
            DateTime.Now;


        await _context.SaveChangesAsync(
            cancellationToken
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