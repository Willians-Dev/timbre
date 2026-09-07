using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.Models;
using Timbre.Api.Services;

namespace Timbre.Api.Controllers;

[ApiController]
[Route("api/marcaciones-faciales")]
[Authorize]
public class MarcacionesFacialesController : ControllerBase
{
    private readonly TimbreDbContext _context;
    private readonly FaceAiService _faceAiService;
    private readonly IdentificacionFacialService
        _identificacionFacialService;
    private readonly MarcacionService
        _marcacionService;
    private readonly FechaHoraService
        _fechaHoraService;
    private readonly AuditoriaService
        _auditoriaService;

    public MarcacionesFacialesController(
        TimbreDbContext context,
        FaceAiService faceAiService,
        IdentificacionFacialService
            identificacionFacialService,
        MarcacionService marcacionService,
        FechaHoraService fechaHoraService,
        AuditoriaService auditoriaService)
    {
        _context = context;
        _faceAiService = faceAiService;
        _identificacionFacialService =
            identificacionFacialService;
        _marcacionService =
            marcacionService;
        _fechaHoraService =
            fechaHoraService;
        _auditoriaService =
            auditoriaService;
    }

    // =====================================================
    // POST:
    // api/marcaciones-faciales/registrar
    //
    // Temporalmente:
    // Administrador / RRHH.
    //
    // Posteriormente el kiosco utilizará
    // una política propia de autenticación.
    // =====================================================

    [HttpPost("registrar")]
    [Authorize(Roles = "Administrador,RRHH")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> Registrar(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        // =================================================
        // 1. VALIDAR FACEAI
        // =================================================

        if (!_faceAiService.EstaHabilitado)
        {
            return BadRequest(new
            {
                mensaje =
                    "El reconocimiento facial está deshabilitado."
            });
        }

        // =================================================
        // 2. VALIDAR IMAGEN
        // =================================================

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
            // 5. NO RECONOCIDO
            // =============================================

            if (!identificacion.Reconocido ||
                !identificacion.IdEmpleado.HasValue)
            {
                return Unauthorized(new
                {
                    reconocido = false,

                    similitud =
                        identificacion.Similitud,

                    umbral =
                        identificacion.Umbral,

                    confianzaDeteccion =
                        identificacion
                            .ConfianzaDeteccion,

                    mensaje =
                        "Rostro no reconocido. " +
                        "No se registró ninguna marcación."
                });
            }

            // =============================================
            // 6. ESTRATEGIA DE REINTENTOS DE EF/NPGSQL
            //
            // Necesario porque Program.cs utiliza:
            //
            // EnableRetryOnFailure(...)
            //
            // Las transacciones manuales deben ejecutarse
            // dentro de CreateExecutionStrategy().
            // =============================================

            var strategy =
                _context.Database
                    .CreateExecutionStrategy();

            object? respuestaFinal = null;

            await strategy.ExecuteAsync(
                async () =>
                {
                    // =====================================
                    // IMPORTANTE:
                    //
                    // Si EF está reintentando este bloque
                    // después de un error transitorio,
                    // limpiamos entidades que pudieron
                    // quedar rastreadas del intento anterior.
                    // =====================================

                    _context.ChangeTracker.Clear();

                    await using var transaction =
                        await _context.Database
                            .BeginTransactionAsync(
                                cancellationToken
                            );

                    // =====================================
                    // 7. REGISTRAR MARCACIÓN
                    // =====================================

                    var marcacion =
                        await _marcacionService
                            .RegistrarAsync(
                                identificacion
                                    .IdEmpleado
                                    .Value,
                                cancellationToken
                            );

                    // =====================================
                    // 8. RESULTADO TÉCNICO DE IA
                    //
                    // No almacenamos:
                    // - imagen
                    // - embedding
                    //
                    // Solamente metadatos técnicos.
                    // =====================================

                    var resultadoTecnico =
                        JsonSerializer.Serialize(
                            new
                            {
                                modelo =
                                    identificacion.Modelo,

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

                    // =====================================
                    // 9. REGISTRAR VALIDACIÓN FACIAL
                    // =====================================

                    var validacion =
                        new ValidacionFacial
                        {
                            IdMarcacion =
                                marcacion.IdMarcacion,

                            IdEmpleado =
                                marcacion.IdEmpleado,

                            Similitud =
                                Convert.ToDecimal(
                                    identificacion
                                        .Similitud
                                ),

                            Umbral =
                                Convert.ToDecimal(
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

                    _context.ValidacionFacial.Add(
                        validacion
                    );

                    await _context.SaveChangesAsync(
                        cancellationToken
                    );

                    // =====================================
                    // 10. AUDITORÍA
                    //
                    // AuditoriaService utiliza el mismo
                    // DbContext Scoped, por lo que queda
                    // dentro de esta transacción.
                    //
                    // Nunca se registra:
                    // - fotografía
                    // - embedding
                    // =====================================

                    await _auditoriaService
                        .RegistrarAsync(
                            User,

                            accion:
                                "MARCACION_FACIAL",

                            entidad:
                                "MarcacionAsistencia",

                            idEntidad:
                                marcacion.IdMarcacion,

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
                                        true
                                },

                            ipOrigen:
                                HttpContext
                                    .Connection
                                    .RemoteIpAddress?
                                    .ToString()
                        );

                    // =====================================
                    // 11. COMMIT
                    //
                    // Aquí quedan confirmados conjuntamente:
                    //
                    // marcacion_asistencia
                    // validacion_facial
                    // auditoria
                    // =====================================

                    await transaction.CommitAsync(
                        cancellationToken
                    );

                    // =====================================
                    // 12. PREPARAR RESPUESTA
                    // =====================================

                    respuestaFinal =
                        new
                        {
                            reconocido =
                                true,

                            idEmpleado =
                                marcacion.IdEmpleado,

                            empleado =
                                marcacion.Empleado,

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
                                marcacion.IdMarcacion,

                            idValidacion =
                                validacion.IdValidacion,

                            fecha =
                                marcacion.Fecha,

                            hora =
                                marcacion.FechaHora
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
                                marcacion.Completa,

                            marcacionesFaltantes =
                                marcacion
                                    .MarcacionesFaltantes,

                            mensaje =
                                $"{marcacion.TipoMarcacion} " +
                                "registrada correctamente " +
                                "mediante reconocimiento facial."
                        };
                }
            );

            // =============================================
            // 13. RESPUESTA FINAL
            // =============================================

            if (respuestaFinal is null)
            {
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
        // ERRORES DE LÓGICA
        //
        // Ejemplos:
        // - ya existe marcación
        // - fuera de horario permitido
        // - jornada inactiva
        // - FaceAI rechazó la imagen
        // =================================================

        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                mensaje =
                    ex.Message
            });
        }

        // =================================================
        // DATOS INVÁLIDOS
        // =================================================

        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                mensaje =
                    ex.Message
            });
        }

        // =================================================
        // ERROR NO CONTROLADO
        // =================================================

        catch (Exception ex)
        {
            return StatusCode(
                StatusCodes
                    .Status500InternalServerError,
                new
                {
                    mensaje =
                        "Ocurrió un error al registrar " +
                        "la marcación facial.",

                    detalle =
                        ex.Message
                }
            );
        }
    }
}