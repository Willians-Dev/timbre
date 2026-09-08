using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Timbre.Api.Data;
using Timbre.Api.DTOs.Usuarios;
using Timbre.Api.Models;
using Timbre.Api.Services;


namespace Timbre.Api.Controllers;


[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly TimbreDbContext
        _context;

    private readonly PasswordService
        _passwordService;

    private readonly FechaHoraService
        _fechaHoraService;

    private readonly AuditoriaService
        _auditoriaService;

    private readonly UsernameService
        _usernameService;

    private readonly ILogger<UsuariosController>
        _logger;


    public UsuariosController(
        TimbreDbContext context,
        PasswordService passwordService,
        FechaHoraService fechaHoraService,
        AuditoriaService auditoriaService,
        UsernameService usernameService,
        ILogger<UsuariosController> logger)
    {
        _context =
            context;

        _passwordService =
            passwordService;

        _fechaHoraService =
            fechaHoraService;

        _auditoriaService =
            auditoriaService;

        _usernameService =
            usernameService;

        _logger =
            logger;
    }


    // =====================================================
    // GET: api/usuarios
    // =====================================================

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioDto>>>
        GetUsuarios(
            CancellationToken cancellationToken)
    {
        var usuarios =
            await _context.Usuario
                .AsNoTracking()
                .OrderBy(u =>
                    u.NombreUsuario)
                .Select(u =>
                    new UsuarioDto
                    {
                        IdUsuario =
                            u.IdUsuario,

                        IdEmpleado =
                            u.IdEmpleado,

                        Empleado =
                            u.IdEmpleadoNavigation.Nombres +
                            " " +
                            u.IdEmpleadoNavigation.Apellidos,

                        IdRol =
                            u.IdRol,

                        Rol =
                            u.IdRolNavigation.Nombre,

                        NombreUsuario =
                            u.NombreUsuario,

                        Activo =
                            u.Activo,

                        UltimoAcceso =
                            u.UltimoAcceso
                    })
                .ToListAsync(
                    cancellationToken
                );


        return Ok(
            usuarios
        );
    }


    // =====================================================
    // GET: api/usuarios/1
    // =====================================================

    [HttpGet("{id:long}")]
    public async Task<ActionResult<UsuarioDto>>
        GetUsuario(
            long id,
            CancellationToken cancellationToken)
    {
        var usuario =
            await _context.Usuario
                .AsNoTracking()
                .Where(u =>
                    u.IdUsuario == id)
                .Select(u =>
                    new UsuarioDto
                    {
                        IdUsuario =
                            u.IdUsuario,

                        IdEmpleado =
                            u.IdEmpleado,

                        Empleado =
                            u.IdEmpleadoNavigation.Nombres +
                            " " +
                            u.IdEmpleadoNavigation.Apellidos,

                        IdRol =
                            u.IdRol,

                        Rol =
                            u.IdRolNavigation.Nombre,

                        NombreUsuario =
                            u.NombreUsuario,

                        Activo =
                            u.Activo,

                        UltimoAcceso =
                            u.UltimoAcceso
                    })
                .FirstOrDefaultAsync(
                    cancellationToken
                );


        if (usuario is null)
        {
            return NotFound(new
            {
                mensaje =
                    "Usuario no encontrado."
            });
        }


        return Ok(
            usuario
        );
    }


    // =====================================================
    // GET: api/usuarios/catalogos/roles
    // =====================================================

    [HttpGet("catalogos/roles")]
    public async Task<ActionResult>
        GetRoles(
            CancellationToken cancellationToken)
    {
        var roles =
            await _context.Rol
                .AsNoTracking()
                .Where(r =>
                    r.Activo)
                .OrderBy(r =>
                    r.Nombre)
                .Select(r =>
                    new
                    {
                        idRol =
                            r.IdRol,

                        nombre =
                            r.Nombre,

                        descripcion =
                            r.Descripcion
                    })
                .ToListAsync(
                    cancellationToken
                );


        return Ok(
            roles
        );
    }


    // =====================================================
    // GET:
    // api/usuarios/catalogos/empleados-disponibles
    // =====================================================

    [HttpGet("catalogos/empleados-disponibles")]
    public async Task<ActionResult>
        GetEmpleadosDisponibles(
            [FromQuery]
            long? idUsuarioActual,
            CancellationToken cancellationToken)
    {
        long? idEmpleadoActual =
            null;


        if (idUsuarioActual.HasValue)
        {
            idEmpleadoActual =
                await _context.Usuario
                    .AsNoTracking()
                    .Where(u =>
                        u.IdUsuario ==
                        idUsuarioActual.Value)
                    .Select(u =>
                        (long?)u.IdEmpleado)
                    .FirstOrDefaultAsync(
                        cancellationToken
                    );
        }


        var empleados =
            await _context.Empleado
                .AsNoTracking()
                .Where(e =>
                    e.Activo)
                .Where(e =>
                    e.IdEmpleado ==
                        idEmpleadoActual ||

                    !_context.Usuario
                        .Any(u =>
                            u.IdEmpleado ==
                            e.IdEmpleado)
                )
                .OrderBy(e =>
                    e.Apellidos)
                .ThenBy(e =>
                    e.Nombres)
                .Select(e =>
                    new
                    {
                        idEmpleado =
                            e.IdEmpleado,

                        identificacion =
                            e.Identificacion,

                        nombreCompleto =
                            e.Nombres +
                            " " +
                            e.Apellidos,

                        area =
                            e.Area,

                        cargo =
                            e.Cargo
                    })
                .ToListAsync(
                    cancellationToken
                );


        return Ok(
            empleados
        );
    }


    // =====================================================
    // POST: api/usuarios
    // =====================================================

    [HttpPost]
    public async Task<ActionResult>
        CrearUsuario(
            [FromBody]
            CrearUsuarioDto dto,
            CancellationToken cancellationToken)
    {
        // =================================================
        // VALIDAR DTO
        // =================================================

        if (dto is null)
        {
            return BadRequest(new
            {
                mensaje =
                    "La solicitud es inválida."
            });
        }


        // =================================================
        // NORMALIZAR / VALIDAR NOMBRE
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
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                mensaje =
                    ex.Message
            });
        }


        // =================================================
        // VALIDAR CONTRASEÑA
        // =================================================

        if (
            string.IsNullOrWhiteSpace(
                dto.Password
            )
        )
        {
            return BadRequest(new
            {
                mensaje =
                    "La contraseña es obligatoria."
            });
        }


        if (
            dto.Password.Length <
            8
        )
        {
            return BadRequest(new
            {
                mensaje =
                    "La contraseña debe tener al menos 8 caracteres."
            });
        }


        // =================================================
        // VALIDAR EMPLEADO
        // =================================================

        var empleado =
            await _context.Empleado
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    e =>
                        e.IdEmpleado ==
                        dto.IdEmpleado,
                    cancellationToken
                );


        if (empleado is null)
        {
            return BadRequest(new
            {
                mensaje =
                    "El empleado no existe."
            });
        }


        if (!empleado.Activo)
        {
            return BadRequest(new
            {
                mensaje =
                    "No se puede crear un usuario para un empleado inactivo."
            });
        }


        // =================================================
        // VALIDAR ROL
        // =================================================

        var rol =
            await _context.Rol
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r =>
                        r.IdRol ==
                            dto.IdRol &&
                        r.Activo,
                    cancellationToken
                );


        if (rol is null)
        {
            return BadRequest(new
            {
                mensaje =
                    "El rol no existe o está inactivo."
            });
        }


        // =================================================
        // UN USUARIO POR EMPLEADO
        // =================================================

        var usuarioEmpleadoExiste =
            await _context.Usuario
                .AsNoTracking()
                .AnyAsync(
                    u =>
                        u.IdEmpleado ==
                        dto.IdEmpleado,
                    cancellationToken
                );


        if (usuarioEmpleadoExiste)
        {
            return Conflict(new
            {
                mensaje =
                    "El empleado ya tiene un usuario asignado."
            });
        }


        // =================================================
        // NOMBRE DE USUARIO ÚNICO
        // =================================================

        var nombreExiste =
            await _context.Usuario
                .AsNoTracking()
                .AnyAsync(
                    u =>
                        u.NombreUsuario ==
                        nombreUsuario,
                    cancellationToken
                );


        if (nombreExiste)
        {
            return Conflict(new
            {
                mensaje =
                    "El nombre de usuario ya se encuentra registrado."
            });
        }


        // =================================================
        // CREAR
        // =================================================

        var usuario =
            new Usuario
            {
                IdEmpleado =
                    dto.IdEmpleado,

                IdRol =
                    dto.IdRol,

                NombreUsuario =
                    nombreUsuario,

                PasswordHash =
                    _passwordService
                        .HashPassword(
                            dto.Password
                        ),

                Activo =
                    true,

                FechaCreacion =
                    _fechaHoraService
                        .AhoraEcuador()
            };


        _context.Usuario.Add(
            usuario
        );


        await _context
            .SaveChangesAsync(
                cancellationToken
            );


        // =================================================
        // AUDITORÍA
        // =================================================

        await _auditoriaService
            .RegistrarAsync(
                User,

                accion:
                    "CREAR_USUARIO",

                entidad:
                    "Usuario",

                idEntidad:
                    usuario.IdUsuario,

                valorAnterior:
                    null,

                valorNuevo:
                    new
                    {
                        usuario.IdUsuario,
                        usuario.IdEmpleado,
                        usuario.IdRol,
                        usuario.NombreUsuario,
                        usuario.Activo
                    },

                ipOrigen:
                    HttpContext
                        .Connection
                        .RemoteIpAddress?
                        .ToString()
            );


        _logger.LogInformation(
            "Usuario {IdUsuario} creado correctamente.",
            usuario.IdUsuario
        );


        return Ok(new
        {
            idUsuario =
                usuario.IdUsuario,

            mensaje =
                "Usuario creado correctamente."
        });
    }


    // =====================================================
    // PUT: api/usuarios/1
    // =====================================================

    [HttpPut("{id:long}")]
    public async Task<ActionResult>
        ActualizarUsuario(
            long id,
            [FromBody]
            ActualizarUsuarioDto dto,
            CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            return BadRequest(new
            {
                mensaje =
                    "La solicitud es inválida."
            });
        }


        var usuario =
            await _context.Usuario
                .FirstOrDefaultAsync(
                    u =>
                        u.IdUsuario == id,
                    cancellationToken
                );


        if (usuario is null)
        {
            return NotFound(new
            {
                mensaje =
                    "Usuario no encontrado."
            });
        }


        // =================================================
        // NORMALIZAR / VALIDAR NOMBRE
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
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                mensaje =
                    ex.Message
            });
        }


        // =================================================
        // VALIDAR ROL DESTINO
        // =================================================

        var rol =
            await _context.Rol
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r =>
                        r.IdRol ==
                            dto.IdRol &&
                        r.Activo,
                    cancellationToken
                );


        if (rol is null)
        {
            return BadRequest(new
            {
                mensaje =
                    "El rol no existe o está inactivo."
            });
        }


        // =================================================
        // NOMBRE ÚNICO
        // =================================================

        var nombreExiste =
            await _context.Usuario
                .AsNoTracking()
                .AnyAsync(
                    u =>
                        u.IdUsuario != id &&
                        u.NombreUsuario ==
                        nombreUsuario,
                    cancellationToken
                );


        if (nombreExiste)
        {
            return Conflict(new
            {
                mensaje =
                    "El nombre de usuario ya se encuentra registrado."
            });
        }


        // =================================================
        // PROTEGER AL ÚLTIMO ADMINISTRADOR
        // =================================================

        var idRolAdministrador =
            await ObtenerIdRolAdministrador(
                cancellationToken
            );


        if (
            idRolAdministrador.HasValue &&
            usuario.IdRol ==
                idRolAdministrador.Value
        )
        {
            var dejaraDeSerAdministrador =
                dto.IdRol !=
                    idRolAdministrador.Value ||
                !dto.Activo;


            if (dejaraDeSerAdministrador)
            {
                var existenOtrosAdministradores =
                    await _context.Usuario
                        .AsNoTracking()
                        .AnyAsync(
                            u =>
                                u.IdUsuario !=
                                    usuario.IdUsuario &&
                                u.IdRol ==
                                    idRolAdministrador.Value &&
                                u.Activo,
                            cancellationToken
                        );


                if (!existenOtrosAdministradores)
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "No se puede modificar este usuario porque es el único Administrador activo del sistema."
                    });
                }
            }
        }


        // =================================================
        // AUDITORÍA - ANTES
        // =================================================

        var valorAnterior =
            new
            {
                usuario.IdUsuario,
                usuario.IdRol,
                usuario.NombreUsuario,
                usuario.Activo
            };


        // =================================================
        // ACTUALIZAR
        // =================================================

        usuario.IdRol =
            dto.IdRol;

        usuario.NombreUsuario =
            nombreUsuario;

        usuario.Activo =
            dto.Activo;

        usuario.FechaModificacion =
            _fechaHoraService
                .AhoraEcuador();


        await _context
            .SaveChangesAsync(
                cancellationToken
            );


        // =================================================
        // AUDITORÍA
        // =================================================

        await _auditoriaService
            .RegistrarAsync(
                User,

                accion:
                    "ACTUALIZAR_USUARIO",

                entidad:
                    "Usuario",

                idEntidad:
                    usuario.IdUsuario,

                valorAnterior:
                    valorAnterior,

                valorNuevo:
                    new
                    {
                        usuario.IdUsuario,
                        usuario.IdRol,
                        usuario.NombreUsuario,
                        usuario.Activo
                    },

                ipOrigen:
                    HttpContext
                        .Connection
                        .RemoteIpAddress?
                        .ToString()
            );


        _logger.LogInformation(
            "Usuario {IdUsuario} actualizado correctamente.",
            usuario.IdUsuario
        );


        return Ok(new
        {
            mensaje =
                "Usuario actualizado correctamente."
        });
    }


    // =====================================================
    // PATCH: api/usuarios/1/estado?activo=false
    // =====================================================

    [HttpPatch("{id:long}/estado")]
    public async Task<ActionResult>
        CambiarEstado(
            long id,
            [FromQuery]
            bool activo,
            CancellationToken cancellationToken)
    {
        var usuario =
            await _context.Usuario
                .FirstOrDefaultAsync(
                    u =>
                        u.IdUsuario == id,
                    cancellationToken
                );


        if (usuario is null)
        {
            return NotFound(new
            {
                mensaje =
                    "Usuario no encontrado."
            });
        }


        if (
            usuario.Activo ==
            activo
        )
        {
            return Ok(new
            {
                mensaje =
                    activo
                        ? "El usuario ya se encuentra activo."
                        : "El usuario ya se encuentra inactivo."
            });
        }


        // =================================================
        // PROTEGER ÚLTIMO ADMINISTRADOR
        // =================================================

        if (!activo)
        {
            var idRolAdministrador =
                await ObtenerIdRolAdministrador(
                    cancellationToken
                );


            if (
                idRolAdministrador.HasValue &&
                usuario.IdRol ==
                    idRolAdministrador.Value
            )
            {
                var existenOtrosAdministradores =
                    await _context.Usuario
                        .AsNoTracking()
                        .AnyAsync(
                            u =>
                                u.IdUsuario !=
                                    usuario.IdUsuario &&
                                u.IdRol ==
                                    idRolAdministrador.Value &&
                                u.Activo,
                            cancellationToken
                        );


                if (!existenOtrosAdministradores)
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "No se puede desactivar este usuario porque es el único Administrador activo del sistema."
                    });
                }
            }
        }


        var estadoAnterior =
            usuario.Activo;


        usuario.Activo =
            activo;

        usuario.FechaModificacion =
            _fechaHoraService
                .AhoraEcuador();


        await _context
            .SaveChangesAsync(
                cancellationToken
            );


        await _auditoriaService
            .RegistrarAsync(
                User,

                accion:
                    "CAMBIAR_ESTADO_USUARIO",

                entidad:
                    "Usuario",

                idEntidad:
                    usuario.IdUsuario,

                valorAnterior:
                    new
                    {
                        Activo =
                            estadoAnterior
                    },

                valorNuevo:
                    new
                    {
                        usuario.Activo
                    },

                ipOrigen:
                    HttpContext
                        .Connection
                        .RemoteIpAddress?
                        .ToString()
            );


        _logger.LogInformation(
            "Estado del usuario {IdUsuario} cambiado a {Activo}.",
            usuario.IdUsuario,
            activo
        );


        return Ok(new
        {
            mensaje =
                activo
                    ? "Usuario activado correctamente."
                    : "Usuario desactivado correctamente."
        });
    }


    // =====================================================
    // POST: api/usuarios/1/reset-password
    // =====================================================

    [HttpPost("{id:long}/reset-password")]
    public async Task<ActionResult>
        ResetPassword(
            long id,
            [FromBody]
            ResetPasswordDto dto,
            CancellationToken cancellationToken)
    {
        if (dto is null)
        {
            return BadRequest(new
            {
                mensaje =
                    "La solicitud es inválida."
            });
        }


        if (
            string.IsNullOrWhiteSpace(
                dto.PasswordNuevo
            )
        )
        {
            return BadRequest(new
            {
                mensaje =
                    "La nueva contraseña es obligatoria."
            });
        }


        if (
            dto.PasswordNuevo.Length <
            8
        )
        {
            return BadRequest(new
            {
                mensaje =
                    "La nueva contraseña debe tener al menos 8 caracteres."
            });
        }


        if (
            dto.PasswordNuevo !=
            dto.ConfirmarPassword
        )
        {
            return BadRequest(new
            {
                mensaje =
                    "La confirmación de la contraseña no coincide."
            });
        }


        var usuario =
            await _context.Usuario
                .FirstOrDefaultAsync(
                    u =>
                        u.IdUsuario == id,
                    cancellationToken
                );


        if (usuario is null)
        {
            return NotFound(new
            {
                mensaje =
                    "Usuario no encontrado."
            });
        }


        usuario.PasswordHash =
            _passwordService
                .HashPassword(
                    dto.PasswordNuevo
                );


        usuario.FechaModificacion =
            _fechaHoraService
                .AhoraEcuador();


        await _context
            .SaveChangesAsync(
                cancellationToken
            );


        /*
         * Nunca se almacenan:
         *
         * - contraseña anterior
         * - contraseña nueva
         * - hashes
         *
         * dentro de auditoría.
         */

        await _auditoriaService
            .RegistrarAsync(
                User,

                accion:
                    "RESET_PASSWORD",

                entidad:
                    "Usuario",

                idEntidad:
                    usuario.IdUsuario,

                valorAnterior:
                    null,

                valorNuevo:
                    new
                    {
                        usuario.IdUsuario,
                        usuario.NombreUsuario,

                        PasswordReseteado =
                            true
                    },

                ipOrigen:
                    HttpContext
                        .Connection
                        .RemoteIpAddress?
                        .ToString()
            );


        _logger.LogInformation(
            "Contraseña del usuario {IdUsuario} restablecida.",
            usuario.IdUsuario
        );


        return Ok(new
        {
            mensaje =
                "Contraseña restablecida correctamente."
        });
    }


    // =====================================================
    // OBTENER ID DEL ROL ADMINISTRADOR
    // =====================================================

    private async Task<long?>
        ObtenerIdRolAdministrador(
            CancellationToken cancellationToken)
    {
        return await _context.Rol
            .AsNoTracking()
            .Where(r =>
                r.Nombre ==
                "Administrador")
            .Select(r =>
                (long?)r.IdRol)
            .FirstOrDefaultAsync(
                cancellationToken
            );
    }
}