using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.DTOs.FaceAI;
using Timbre.Api.Models;
using Timbre.Api.Services;

namespace Timbre.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RostrosController : ControllerBase
{
    private readonly TimbreDbContext _context;
    private readonly FaceAiService _faceAiService;
    private readonly FechaHoraService _fechaHoraService;
    private readonly AuditoriaService _auditoriaService;
    private readonly IdentificacionFacialService _identificacionFacialService;

    public RostrosController(
        TimbreDbContext context,
        FaceAiService faceAiService,
        IdentificacionFacialService identificacionFacialService,
        FechaHoraService fechaHoraService,
        AuditoriaService auditoriaService)
    {
        _context = context;
        _faceAiService = faceAiService;
        _identificacionFacialService =
            identificacionFacialService;
        _fechaHoraService = fechaHoraService;
        _auditoriaService = auditoriaService;
    }

    // =====================================================
    // POST:
    // api/rostros/empleado/1/enrolar
    //
    // Solo Administrador o RRHH.
    // =====================================================
    [HttpPost("empleado/{idEmpleado:long}/enrolar")]
    [Authorize(Roles = "Administrador,RRHH")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> EnrolarEmpleado(
        long idEmpleado,
        IFormFile file1,
        IFormFile file2,
        IFormFile file3,
        CancellationToken cancellationToken)
    {
        if (!_faceAiService.EstaHabilitado)
        {
            return BadRequest(new
            {
                mensaje =
                    "El reconocimiento facial está deshabilitado."
            });
        }

        // =================================================
        // VALIDAR EMPLEADO
        //
        // IMPORTANTE:
        // AsNoTracking evita que EF relacione automáticamente
        // el nuevo RostroEmpleado con un principal trackeado.
        // =================================================
        var empleado =
            await _context.Empleado
                .AsNoTracking()
                .Where(e =>
                    e.IdEmpleado == idEmpleado)
                .Select(e => new
                {
                    e.IdEmpleado,
                    e.Nombres,
                    e.Apellidos,
                    e.Activo
                })
                .FirstOrDefaultAsync(
                    cancellationToken
                );

        if (empleado is null)
        {
            return NotFound(new
            {
                mensaje =
                    "Empleado no encontrado."
            });
        }

        if (!empleado.Activo)
        {
            return BadRequest(new
            {
                mensaje =
                    "El empleado se encuentra inactivo."
            });
        }

        // =================================================
        // VALIDAR ARCHIVOS
        // =================================================
        if (file1 is null ||
            file1.Length == 0 ||
            file2 is null ||
            file2.Length == 0 ||
            file3 is null ||
            file3.Length == 0)
        {
            return BadRequest(new
            {
                mensaje =
                    "Debe proporcionar tres imágenes para el enrolamiento."
            });
        }

        try
        {
            // =================================================
            // PROCESAR LAS 3 MUESTRAS EN FACEAI
            //
            // FaceAI valida los rostros y devuelve
            // el embedding maestro.
            // =================================================
            var resultadoFaceAi =
                await _faceAiService
                    .EnrolarAsync(
                        file1,
                        file2,
                        file3,
                        cancellationToken
                    );

            var ahora =
                _fechaHoraService
                    .AhoraEcuador();

            // =================================================
            // TRANSACCIÓN CON ESTRATEGIA DE REINTENTO
            //
            // Necesario porque PostgreSQL Azure está usando
            // EnableRetryOnFailure().
            // =================================================
            var strategy =
                _context.Database
                    .CreateExecutionStrategy();

            EnrolamientoRostroResultadoDto? respuesta =
                null;

            await strategy.ExecuteAsync(
                async () =>
                {
                    /*
                     * Limpiamos cualquier entidad que pudiera
                     * haber quedado trackeada previamente.
                     */
                    _context.ChangeTracker.Clear();

                    await using var transaction =
                        await _context.Database
                            .BeginTransactionAsync(
                                cancellationToken
                            );

                    try
                    {
                        // =========================================
                        // SABER SI ERA ENROLAMIENTO O RE-ENROLAMIENTO
                        // =========================================
                        var teniaRostroActivo =
                            await _context
                                .RostroEmpleado
                                .AsNoTracking()
                                .AnyAsync(
                                    r =>
                                        r.IdEmpleado == idEmpleado &&
                                        r.Activo,
                                    cancellationToken
                                );

                        // =========================================
                        // DESACTIVAR ROSTROS ANTERIORES
                        //
                        // ExecuteUpdateAsync modifica directamente
                        // la BD y NO carga los RostroEmpleado al
                        // ChangeTracker.
                        //
                        // Esto evita el conflicto de relación que
                        // estaba generando Entity Framework.
                        // =========================================
                        await _context
                            .RostroEmpleado
                            .Where(r =>
                                r.IdEmpleado == idEmpleado &&
                                r.Activo)
                            .ExecuteUpdateAsync(
                                setters =>
                                    setters

                                        .SetProperty(
                                            r => r.Activo,
                                            false
                                        )

                                        .SetProperty(
                                            r => r.FechaModificacion,
                                            ahora
                                        ),
                                cancellationToken
                            );

                        // =========================================
                        // NUEVO ROSTRO
                        // =========================================
                        var nuevoRostro =
                            new RostroEmpleado
                            {
                                IdEmpleado =
                                    idEmpleado,

                                RutaImagen =
                                    null,

                                Embedding =
                                    resultadoFaceAi
                                        .Embedding,

                                Modelo =
                                    "OpenCV SFace",

                                DimensionEmbedding =
                                    resultadoFaceAi
                                        .DimensionEmbedding,

                                Activo =
                                    true,

                                FechaRegistro =
                                    ahora,

                                FechaModificacion =
                                    null
                            };

                        /*
                         * Solo agregamos el nuevo dependiente.
                         *
                         * No asignamos:
                         *
                         * nuevoRostro.IdEmpleadoNavigation = empleado
                         *
                         * ni modificamos colecciones de navegación.
                         */
                        _context.RostroEmpleado.Add(
                            nuevoRostro
                        );

                        await _context.SaveChangesAsync(
                            cancellationToken
                        );

                        // =========================================
                        // AUDITORÍA
                        //
                        // NO almacenar embedding ni fotografías.
                        // =========================================
                        var accion =
                            teniaRostroActivo
                                ? "REENROLAR_ROSTRO"
                                : "ENROLAR_ROSTRO";

                        await _auditoriaService
                            .RegistrarAsync(
                                User,
                                accion:
                                    accion,
                                entidad:
                                    "RostroEmpleado",
                                idEntidad:
                                    nuevoRostro.IdRostro,
                                valorAnterior:
                                    null,
                                valorNuevo:
                                    new
                                    {
                                        nuevoRostro.IdRostro,

                                        nuevoRostro.IdEmpleado,

                                        nuevoRostro.Modelo,

                                        nuevoRostro
                                            .DimensionEmbedding,

                                        CantidadMuestras =
                                            resultadoFaceAi
                                                .CantidadMuestras,

                                        RostroRegistrado =
                                            true
                                    },
                                ipOrigen:
                                    HttpContext
                                        .Connection
                                        .RemoteIpAddress?
                                        .ToString()
                            );

                        respuesta =
                            new EnrolamientoRostroResultadoDto
                            {
                                IdRostro =
                                    nuevoRostro.IdRostro,

                                IdEmpleado =
                                    empleado.IdEmpleado,

                                Empleado =
                                    $"{empleado.Nombres} " +
                                    $"{empleado.Apellidos}",

                                EnrolamientoValido =
                                    true,

                                CantidadMuestras =
                                    resultadoFaceAi
                                        .CantidadMuestras,

                                DimensionEmbedding =
                                    resultadoFaceAi
                                        .DimensionEmbedding,

                                Modelo =
                                    nuevoRostro.Modelo
                                    ?? string.Empty,

                                Mensaje =
                                    "Rostro del empleado enrolado correctamente."
                            };

                        await transaction.CommitAsync(
                            cancellationToken
                        );
                    }
                    catch
                    {
                        await transaction.RollbackAsync(
                            cancellationToken
                        );

                        throw;
                    }
                }
            );

            return Ok(
                respuesta
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                mensaje =
                    ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                mensaje =
                    ex.Message
            });
        }
    }

    // =====================================================
    // GET:
    // api/rostros/empleado/1
    //
    // No devuelve embedding.
    // =====================================================
    [HttpGet("empleado/{idEmpleado:long}")]
    [Authorize(Roles = "Administrador,RRHH")]
    public async Task<ActionResult> GetRostroEmpleado(
        long idEmpleado,
        CancellationToken cancellationToken)
    {
        var rostro =
            await _context
                .RostroEmpleado
                .AsNoTracking()
                .Where(r =>
                    r.IdEmpleado == idEmpleado &&
                    r.Activo)
                .Select(r => new
                {
                    r.IdRostro,

                    r.IdEmpleado,

                    r.Modelo,

                    r.DimensionEmbedding,

                    r.Activo,

                    r.FechaRegistro,

                    r.FechaModificacion
                })
                .FirstOrDefaultAsync(
                    cancellationToken
                );

        if (rostro is null)
        {
            return NotFound(new
            {
                mensaje =
                    "El empleado no tiene un rostro activo registrado."
            });
        }

        return Ok(
            rostro
        );
    }

    // =====================================================
    // POST:
    // api/rostros/identificar
    // =====================================================
    [HttpPost("identificar")]
    [Authorize(Roles = "Administrador,RRHH")]
    [Consumes("multipart/form-data")]
    public async Task<
        ActionResult<IdentificacionFacialResultadoDto>>
        Identificar(
            IFormFile file,
            CancellationToken cancellationToken)
    {
        if (!_faceAiService.EstaHabilitado)
        {
            return BadRequest(new
            {
                mensaje =
                    "El reconocimiento facial está deshabilitado."
            });
        }

        if (file is null ||
            file.Length == 0)
        {
            return BadRequest(new
            {
                mensaje =
                    "Debe proporcionar una imagen."
            });
        }

        try
        {
            var resultadoFaceAi =
                await _faceAiService
                    .GenerarEmbeddingAsync(
                        file,
                        cancellationToken
                    );

            var resultado =
                await _identificacionFacialService
                    .IdentificarAsync(
                        resultadoFaceAi.Embedding,
                        resultadoFaceAi.Confianza,
                        cancellationToken
                    );

            return Ok(
                resultado
            );
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                mensaje =
                    ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                mensaje =
                    ex.Message
            });
        }
    }
}