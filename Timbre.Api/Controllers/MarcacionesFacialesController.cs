using System.Text.Json;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

using Timbre.Api.Data;
using Timbre.Api.Models;
using Timbre.Api.Services;


namespace Timbre.Api.Controllers;


[ApiController]
[Route("api/marcaciones-faciales")]
[Authorize]
public class MarcacionesFacialesController
    : ControllerBase
{
    private readonly TimbreDbContext
        _context;

    private readonly FaceAiService
        _faceAiService;

    private readonly IdentificacionFacialService
        _identificacionFacialService;

    private readonly MarcacionService
        _marcacionService;

    private readonly FechaHoraService
        _fechaHoraService;

    private readonly AuditoriaService
        _auditoriaService;

    private readonly ILogger<MarcacionesFacialesController>
        _logger;


    // =====================================================
    // HARDENING - IMAGEN
    // =====================================================

    private const long MaximoBytesImagen =
        5L * 1024L * 1024L;


    private static readonly HashSet<string>
        TiposMimePermitidos =
        new(
            StringComparer.OrdinalIgnoreCase
        )
        {
            "image/jpeg",
            "image/png"
        };


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public MarcacionesFacialesController(
        TimbreDbContext context,
        FaceAiService faceAiService,
        IdentificacionFacialService identificacionFacialService,
        MarcacionService marcacionService,
        FechaHoraService fechaHoraService,
        AuditoriaService auditoriaService,
        ILogger<MarcacionesFacialesController> logger)
    {
        _context =
            context;

        _faceAiService =
            faceAiService;

        _identificacionFacialService =
            identificacionFacialService;

        _marcacionService =
            marcacionService;

        _fechaHoraService =
            fechaHoraService;

        _auditoriaService =
            auditoriaService;

        _logger =
            logger;
    }


    // =====================================================
    // POST:
    // api/marcaciones-faciales/registrar
    //
    // PÚBLICO
    //
    // Hardening:
    //
    // - AllowAnonymous únicamente para kiosko
    // - Rate limiting
    // - Request máximo 6 MB
    // - Imagen máxima 5 MB
    // - Solo JPEG / PNG
    // - Firma binaria
    //
    // No se almacena:
    //
    // - fotografía
    // - embedding de la captura
    // =====================================================

    [HttpPost("registrar")]
    [AllowAnonymous]
    [EnableRateLimiting("KioscoFacial")]
    [Consumes("multipart/form-data")]

    [RequestSizeLimit(
        6L * 1024L * 1024L
    )]

    [RequestFormLimits(
        MultipartBodyLengthLimit =
            6L * 1024L * 1024L
    )]

    public async Task<ActionResult>
        Registrar(
            IFormFile file,
            CancellationToken cancellationToken)
    {
        // =================================================
        // 1. VALIDAR FACEAI
        // =================================================

        if (
            !_faceAiService
                .EstaHabilitado
        )
        {
            return BadRequest(new
            {
                mensaje =
                    "El reconocimiento facial está deshabilitado."
            });
        }


        // =================================================
        // 2. VALIDAR ARCHIVO
        // =================================================

        if (
            file is null ||
            file.Length == 0
        )
        {
            return BadRequest(new
            {
                mensaje =
                    "Debe proporcionar una imagen."
            });
        }


        // =================================================
        // 2.1 TAMAÑO MÁXIMO
        // =================================================

        if (
            file.Length >
            MaximoBytesImagen
        )
        {
            return StatusCode(
                StatusCodes
                    .Status413PayloadTooLarge,

                new
                {
                    mensaje =
                        "La imagen supera el tamaño máximo permitido de 5 MB."
                }
            );
        }


        // =================================================
        // 2.2 TIPO MIME
        // =================================================

        if (
            string.IsNullOrWhiteSpace(
                file.ContentType
            ) ||
            !TiposMimePermitidos
                .Contains(
                    file.ContentType
                )
        )
        {
            return BadRequest(new
            {
                mensaje =
                    "Formato de imagen no permitido. " +
                    "Utilice únicamente archivos JPEG o PNG."
            });
        }


        // =================================================
        // 2.3 FIRMA BINARIA
        // =================================================

        var imagenValida =
            await EsImagenValidaAsync(
                file,
                cancellationToken
            );


        if (!imagenValida)
        {
            return BadRequest(new
            {
                mensaje =
                    "El archivo proporcionado no contiene una imagen JPEG o PNG válida."
            });
        }


        try
        {
            // =============================================
            // 3. GENERAR EMBEDDING CON FACEAI
            // =============================================

            var resultadoFaceAi =
                await _faceAiService
                    .GenerarEmbeddingAsync(
                        file,
                        cancellationToken
                    );


            // =============================================
            // 4. IDENTIFICACIÓN 1:N
            // =============================================

            var identificacion =
                await _identificacionFacialService
                    .IdentificarAsync(
                        resultadoFaceAi.Embedding,
                        resultadoFaceAi.Confianza,
                        cancellationToken
                    );


            // =============================================
            // 5. ROSTRO NO RECONOCIDO
            //
            // Se mantiene 422.
            //
            // No usar 401 porque el frontend puede
            // interpretarlo como sesión expirada.
            // =============================================

            if (
                !identificacion.Reconocido ||
                !identificacion
                    .IdEmpleado
                    .HasValue
            )
            {
                _logger.LogWarning(
                    "Intento de marcación facial sin coincidencia. " +
                    "Similitud: {Similitud}, Umbral: {Umbral}, IP: {Ip}.",

                    identificacion.Similitud,
                    identificacion.Umbral,

                    HttpContext
                        .Connection
                        .RemoteIpAddress?
                        .ToString()
                );


                return UnprocessableEntity(
                    new
                    {
                        reconocido =
                            false,

                        similitud =
                            identificacion
                                .Similitud,

                        umbral =
                            identificacion
                                .Umbral,

                        confianzaDeteccion =
                            identificacion
                                .ConfianzaDeteccion,

                        mensaje =
                            "Rostro no reconocido. " +
                            "No se registró ninguna marcación."
                    }
                );
            }


            // =============================================
            // 6. ESTRATEGIA DE REINTENTOS EF / NPGSQL
            // =============================================

            var strategy =
                _context.Database
                    .CreateExecutionStrategy();


            object? respuestaFinal =
                null;


            await strategy
                .ExecuteAsync(
                    async () =>
                    {
                        // =================================
                        // LIMPIAR TRACKING
                        // =================================

                        _context
                            .ChangeTracker
                            .Clear();


                        await using var transaction =
                            await _context
                                .Database
                                .BeginTransactionAsync(
                                    cancellationToken
                                );


                        try
                        {
                            // =============================
                            // 7. REGISTRAR MARCACIÓN
                            // =============================

                            var marcacion =
                                await _marcacionService
                                    .RegistrarAsync(
                                        identificacion
                                            .IdEmpleado
                                            .Value,

                                        cancellationToken
                                    );


                            // =============================
                            // 8. RESULTADO TÉCNICO IA
                            //
                            // Solo metadatos.
                            //
                            // NO:
                            // - imagen
                            // - embedding
                            // =============================

                            var resultadoTecnico =
                                JsonSerializer
                                    .Serialize(
                                        new
                                        {
                                            modelo =
                                                identificacion
                                                    .Modelo,

                                            confianzaDeteccion =
                                                identificacion
                                                    .ConfianzaDeteccion,

                                            dimensionEmbedding =
                                                resultadoFaceAi
                                                    .DimensionEmbedding,

                                            metodo =
                                                "cosine_similarity",

                                            cantidadRostros =
                                                resultadoFaceAi
                                                    .CantidadRostros
                                        }
                                    );


                            // =============================
                            // 9. VALIDACIÓN FACIAL
                            // =============================

                            var validacion =
                                new ValidacionFacial
                                {
                                    IdMarcacion =
                                        marcacion
                                            .IdMarcacion,

                                    IdEmpleado =
                                        marcacion
                                            .IdEmpleado,

                                    Similitud =
                                        Convert
                                            .ToDecimal(
                                                identificacion
                                                    .Similitud
                                            ),

                                    Umbral =
                                        Convert
                                            .ToDecimal(
                                                identificacion
                                                    .Umbral
                                            ),

                                    Aprobado =
                                        true,

                                    ResultadoTecnico =
                                        resultadoTecnico,

                                    FechaValidacion =
                                        _fechaHoraService
                                            .AhoraEcuador()
                                };


                            _context
                                .ValidacionFacial
                                .Add(
                                    validacion
                                );


                            await _context
                                .SaveChangesAsync(
                                    cancellationToken
                                );


                            // =============================
                            // 10. AUDITORÍA
                            // =============================

                            await _auditoriaService
                                .RegistrarAsync(
                                    User,

                                    accion:
                                        "MARCACION_FACIAL",

                                    entidad:
                                        "MarcacionAsistencia",

                                    idEntidad:
                                        marcacion
                                            .IdMarcacion,

                                    valorAnterior:
                                        null,

                                    valorNuevo:
                                        new
                                        {
                                            marcacion
                                                .IdMarcacion,

                                            marcacion
                                                .IdEmpleado,

                                            marcacion
                                                .TipoMarcacion,

                                            Similitud =
                                                identificacion
                                                    .Similitud,

                                            Umbral =
                                                identificacion
                                                    .Umbral,

                                            ValidacionFacial =
                                                true,

                                            Origen =
                                                "KIOSCO_PUBLICO"
                                        },

                                    ipOrigen:
                                        HttpContext
                                            .Connection
                                            .RemoteIpAddress?
                                            .ToString()
                                );


                            // =============================
                            // 11. COMMIT
                            // =============================

                            await transaction
                                .CommitAsync(
                                    cancellationToken
                                );


                            // =============================
                            // LOG OPERATIVO
                            // =============================

                            _logger.LogInformation(
                                "Marcación facial registrada. " +
                                "IdMarcacion: {IdMarcacion}, " +
                                "IdEmpleado: {IdEmpleado}, " +
                                "Tipo: {TipoMarcacion}, " +
                                "Similitud: {Similitud}.",

                                marcacion.IdMarcacion,
                                marcacion.IdEmpleado,
                                marcacion.TipoMarcacion,
                                identificacion.Similitud
                            );


                            // =============================
                            // 12. RESPUESTA
                            // =============================

                            respuestaFinal =
                                new
                                {
                                    reconocido =
                                        true,

                                    idEmpleado =
                                        marcacion
                                            .IdEmpleado,

                                    empleado =
                                        marcacion
                                            .Empleado,

                                    similitud =
                                        identificacion
                                            .Similitud,

                                    umbral =
                                        identificacion
                                            .Umbral,

                                    confianzaDeteccion =
                                        identificacion
                                            .ConfianzaDeteccion,

                                    idMarcacion =
                                        marcacion
                                            .IdMarcacion,

                                    idValidacion =
                                        validacion
                                            .IdValidacion,

                                    fecha =
                                        marcacion
                                            .Fecha,

                                    hora =
                                        marcacion
                                            .FechaHora
                                            .ToString(
                                                "HH:mm:ss"
                                            ),

                                    tipoMarcacion =
                                        marcacion
                                            .TipoMarcacion,

                                    estadoEntrada =
                                        marcacion
                                            .EstadoEntrada,

                                    completa =
                                        marcacion
                                            .Completa,

                                    marcacionesFaltantes =
                                        marcacion
                                            .MarcacionesFaltantes,

                                    mensaje =
                                        marcacion
                                            .Mensaje
                                };
                        }
                        catch
                        {
                            await transaction
                                .RollbackAsync(
                                    cancellationToken
                                );

                            throw;
                        }
                    }
                );


            // =============================================
            // 13. RESPUESTA FINAL
            // =============================================

            if (
                respuestaFinal is null
            )
            {
                _logger.LogError(
                    "La operación facial terminó sin generar respuesta final."
                );


                return StatusCode(
                    StatusCodes
                        .Status500InternalServerError,

                    new
                    {
                        mensaje =
                            "No fue posible completar la marcación facial."
                    }
                );
            }


            return Ok(
                respuestaFinal
            );
        }


        // =================================================
        // REGLAS DE NEGOCIO
        // =================================================

        catch (
            InvalidOperationException ex
        )
        {
            _logger.LogWarning(
                "Regla de negocio impidió marcación facial: {Mensaje}",
                ex.Message
            );


            return Conflict(
                new
                {
                    mensaje =
                        ex.Message
                }
            );
        }


        // =================================================
        // DATOS INVÁLIDOS
        // =================================================

        catch (
            ArgumentException ex
        )
        {
            _logger.LogWarning(
                "Datos inválidos en marcación facial: {Mensaje}",
                ex.Message
            );


            return BadRequest(
                new
                {
                    mensaje =
                        ex.Message
                }
            );
        }


        // =================================================
        // ERROR NO CONTROLADO
        //
        // El detalle queda únicamente en logs.
        // No se expone ex.Message al cliente.
        // =================================================

        catch (
            Exception ex
        )
        {
            _logger.LogError(
                ex,

                "Error no controlado durante una marcación facial. " +
                "IP: {Ip}.",

                HttpContext
                    .Connection
                    .RemoteIpAddress?
                    .ToString()
            );


            return StatusCode(
                StatusCodes
                    .Status500InternalServerError,

                new
                {
                    mensaje =
                        "Ocurrió un error al registrar la marcación facial."
                }
            );
        }
    }


    // =====================================================
    // VALIDAR FIRMA BINARIA
    //
    // JPEG:
    // FF D8 FF
    //
    // PNG:
    // 89 50 4E 47 0D 0A 1A 0A
    // =====================================================

    private static async Task<bool>
        EsImagenValidaAsync(
            IFormFile file,
            CancellationToken cancellationToken)
    {
        if (
            file.Length <
            8
        )
        {
            return false;
        }


        var buffer =
            new byte[8];


        await using var stream =
            file.OpenReadStream();


        var bytesLeidos =
            await stream
                .ReadAsync(
                    buffer.AsMemory(
                        0,
                        buffer.Length
                    ),

                    cancellationToken
                );


        if (
            bytesLeidos <
            8
        )
        {
            return false;
        }


        // =================================================
        // JPEG
        // =================================================

        var esJpeg =
            buffer[0] == 0xFF &&
            buffer[1] == 0xD8 &&
            buffer[2] == 0xFF;


        if (esJpeg)
        {
            return true;
        }


        // =================================================
        // PNG
        // =================================================

        var esPng =
            buffer[0] == 0x89 &&
            buffer[1] == 0x50 &&
            buffer[2] == 0x4E &&
            buffer[3] == 0x47 &&
            buffer[4] == 0x0D &&
            buffer[5] == 0x0A &&
            buffer[6] == 0x1A &&
            buffer[7] == 0x0A;


        return esPng;
    }
}