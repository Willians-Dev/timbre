using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.DTOs.Usuarios;
using Timbre.Api.Services;

namespace Timbre.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CuentaController : ControllerBase
{
    private readonly TimbreDbContext _context;
    private readonly PasswordService _passwordService;
    private readonly FechaHoraService _fechaHoraService;
    private readonly AuditoriaService _auditoriaService;

    public CuentaController(
        TimbreDbContext context,
        PasswordService passwordService,
        FechaHoraService fechaHoraService,
        AuditoriaService auditoriaService)
    {
        _context = context;
        _passwordService = passwordService;
        _fechaHoraService = fechaHoraService;
        _auditoriaService = auditoriaService;
    }

    // =====================================================
    // GET: api/cuenta
    //
    // Información básica del usuario autenticado.
    // =====================================================
    [HttpGet]
    public async Task<ActionResult> GetCuenta()
    {
        var idUsuarioClaim =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!long.TryParse(
            idUsuarioClaim,
            out var idUsuario))
        {
            return Unauthorized();
        }

        var usuario = await _context.Usuario
            .AsNoTracking()
            .Where(u =>
                u.IdUsuario == idUsuario &&
                u.Activo)
            .Select(u => new
            {
                u.IdUsuario,

                u.IdEmpleado,

                empleado =
                    u.IdEmpleadoNavigation.Nombres +
                    " " +
                    u.IdEmpleadoNavigation.Apellidos,

                u.NombreUsuario,

                idRol =
                    u.IdRol,

                rol =
                    u.IdRolNavigation.Nombre,

                u.UltimoAcceso
            })
            .FirstOrDefaultAsync();

        if (usuario is null)
        {
            return Unauthorized();
        }

        return Ok(usuario);
    }

    // =====================================================
    // POST: api/cuenta/cambiar-password
    //
    // Cualquier usuario autenticado puede cambiar
    // únicamente su propia contraseña.
    // =====================================================
    [HttpPost("cambiar-password")]
    public async Task<ActionResult> CambiarPassword(
        [FromBody] CambiarPasswordDto dto)
    {
        var idUsuarioClaim =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!long.TryParse(
            idUsuarioClaim,
            out var idUsuario))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(
            dto.PasswordActual))
        {
            return BadRequest(new
            {
                mensaje =
                    "La contraseña actual es obligatoria."
            });
        }

        if (string.IsNullOrWhiteSpace(
            dto.PasswordNuevo))
        {
            return BadRequest(new
            {
                mensaje =
                    "La nueva contraseña es obligatoria."
            });
        }

        if (dto.PasswordNuevo.Length < 8)
        {
            return BadRequest(new
            {
                mensaje =
                    "La nueva contraseña debe tener al menos 8 caracteres."
            });
        }

        if (dto.PasswordNuevo !=
            dto.ConfirmarPassword)
        {
            return BadRequest(new
            {
                mensaje =
                    "La confirmación de la contraseña no coincide."
            });
        }

        var usuario = await _context.Usuario
            .FirstOrDefaultAsync(u =>
                u.IdUsuario == idUsuario &&
                u.Activo);

        if (usuario is null)
        {
            return Unauthorized();
        }

        var passwordActualCorrecto =
            _passwordService.VerificarPassword(
                usuario.PasswordHash,
                dto.PasswordActual);

        if (!passwordActualCorrecto)
        {
            return BadRequest(new
            {
                mensaje =
                    "La contraseña actual es incorrecta."
            });
        }

        var esMismaPassword =
            _passwordService.VerificarPassword(
                usuario.PasswordHash,
                dto.PasswordNuevo);

        if (esMismaPassword)
        {
            return BadRequest(new
            {
                mensaje =
                    "La nueva contraseña debe ser diferente de la contraseña actual."
            });
        }

        usuario.PasswordHash =
            _passwordService.HashPassword(
                dto.PasswordNuevo);

        usuario.FechaModificacion =
            _fechaHoraService.AhoraEcuador();

        await _context.SaveChangesAsync();

        // Nunca se registra la contraseña ni el hash
        // dentro de la auditoría.
        await _auditoriaService.RegistrarAsync(
            User,
            accion: "CAMBIAR_PASSWORD",
            entidad: "Usuario",
            idEntidad: usuario.IdUsuario,
            valorAnterior: null,
            valorNuevo: new
            {
                usuario.IdUsuario,
                usuario.NombreUsuario,
                PasswordModificado = true
            },
            ipOrigen:
                HttpContext.Connection
                    .RemoteIpAddress?
                    .ToString());

        return Ok(new
        {
            mensaje =
                "Contraseña actualizada correctamente."
        });
    }
}