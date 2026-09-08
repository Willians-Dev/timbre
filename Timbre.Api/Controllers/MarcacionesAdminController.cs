using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.DTOs.Marcaciones;
using Timbre.Api.Models;
using Timbre.Api.Services;

namespace Timbre.Api.Controllers;


[Authorize(Roles = "Administrador,RRHH")]
[ApiController]
[Route("api/marcaciones-admin")]
public class MarcacionesAdminController : ControllerBase
{
    private readonly TimbreDbContext
        _context;


    private readonly FechaHoraService
        _fechaHoraService;


    private readonly AuditoriaService
        _auditoriaService;


    public MarcacionesAdminController(
        TimbreDbContext context,
        FechaHoraService fechaHoraService,
        AuditoriaService auditoriaService)
    {
        _context =
            context;


        _fechaHoraService =
            fechaHoraService;


        _auditoriaService =
            auditoriaService;
    }


    // =====================================================
    // POST:
    // api/marcaciones-admin/correccion
    //
    // Crea una marcación manual.
    //
    // Se conserva por compatibilidad con la funcionalidad
    // administrativa que ya existe.
    // =====================================================

    [HttpPost("correccion")]
    public async Task<ActionResult>
        CrearMarcacionManual(
            [FromBody] CorreccionManualMarcacionDto dto)
    {
        var tiposPermitidos =
            ObtenerTiposPermitidos();


        if (
            !tiposPermitidos.Contains(
                dto.TipoMarcacion
            )
        )
        {
            return BadRequest(
                new
                {
                    mensaje =
                        "El tipo de marcación no es válido."
                }
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                dto.Motivo
            )
        )
        {
            return BadRequest(
                new
                {
                    mensaje =
                        "El motivo de la corrección es obligatorio."
                }
            );
        }


        var empleado =
            await _context
                .Empleado
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    e =>
                        e.IdEmpleado ==
                        dto.IdEmpleado
                );


        if (
            empleado is null
        )
        {
            return NotFound(
                new
                {
                    mensaje =
                        "Empleado no encontrado."
                }
            );
        }


        var yaExiste =
            await _context
                .MarcacionAsistencia
                .AnyAsync(
                    m =>
                        m.IdEmpleado ==
                            dto.IdEmpleado &&

                        m.FechaMarcacion ==
                            dto.Fecha &&

                        m.TipoMarcacion ==
                            dto.TipoMarcacion &&

                        m.EstadoMarcacion ==
                            "Activa"
                );


        if (
            yaExiste
        )
        {
            return Conflict(
                new
                {
                    mensaje =
                        $"Ya existe una marcación activa de tipo " +
                        $"{ObtenerNombreTipo(dto.TipoMarcacion)} " +
                        "para esa fecha."
                }
            );
        }


        var fechaHora =
            dto.Fecha
                .ToDateTime(
                    dto.Hora
                );


        var motivo =
            dto.Motivo
                .Trim();


        var marcacion =
            new MarcacionAsistencia
            {
                IdEmpleado =
                    dto.IdEmpleado,

                FechaMarcacion =
                    dto.Fecha,

                FechaHora =
                    fechaHora,

                TipoMarcacion =
                    dto.TipoMarcacion,

                EstadoMarcacion =
                    "Activa",

                Observacion =
                    $"Corrección manual: {motivo}",

                FechaCreacion =
                    _fechaHoraService
                        .AhoraEcuador()
            };


        _context
            .MarcacionAsistencia
            .Add(
                marcacion
            );


        await _context
            .SaveChangesAsync();


        await _auditoriaService
            .RegistrarAsync(
                User,

                accion:
                    "CREAR_MARCACION_MANUAL",

                entidad:
                    "MarcacionAsistencia",

                idEntidad:
                    marcacion.IdMarcacion,

                valorAnterior:
                    null,

                valorNuevo:
                    new
                    {
                        marcacion.IdMarcacion,
                        marcacion.IdEmpleado,
                        marcacion.FechaMarcacion,
                        marcacion.FechaHora,
                        marcacion.TipoMarcacion,
                        marcacion.EstadoMarcacion,
                        marcacion.Observacion
                    },

                ipOrigen:
                    HttpContext
                        .Connection
                        .RemoteIpAddress?
                        .ToString()
            );


        return Ok(
            new
            {
                idMarcacion =
                    marcacion.IdMarcacion,

                mensaje =
                    "Marcación manual registrada correctamente."
            }
        );
    }


    // =====================================================
    // POST:
    // api/marcaciones-admin/regularizar
    //
    // Agrega una MARCACIÓN FALTANTE.
    //
    // Reglas:
    //
    // - Solo Administrador / RRHH.
    // - Motivo obligatorio.
    // - No fechas futuras.
    // - No duplicados activos.
    // - Respeta secuencia cronológica.
    // - No modifica marcaciones existentes.
    // - Queda registrada en Auditoría.
    // =====================================================

    [HttpPost("regularizar")]
    public async Task<ActionResult>
        RegularizarMarcacion(
            [FromBody] RegularizarMarcacionDto dto)
    {
        // =================================================
        // VALIDACIONES BÁSICAS
        // =================================================

        if (
            dto.IdEmpleado <=
            0
        )
        {
            return BadRequest(
                new
                {
                    mensaje =
                        "Debe seleccionar un empleado."
                }
            );
        }


        var tiposPermitidos =
            ObtenerTiposPermitidos();


        if (
            string.IsNullOrWhiteSpace(
                dto.TipoMarcacion
            ) ||
            !tiposPermitidos.Contains(
                dto.TipoMarcacion
            )
        )
        {
            return BadRequest(
                new
                {
                    mensaje =
                        "El tipo de marcación no es válido."
                }
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                dto.Motivo
            )
        )
        {
            return BadRequest(
                new
                {
                    mensaje =
                        "El motivo de la regularización es obligatorio."
                }
            );
        }


        var motivo =
            dto.Motivo
                .Trim();


        if (
            motivo.Length <
            5
        )
        {
            return BadRequest(
                new
                {
                    mensaje =
                        "Ingrese un motivo de regularización más descriptivo."
                }
            );
        }


        // Dejamos espacio para el prefijo que almacenaremos
        // en observación.

        if (
            motivo.Length >
            440
        )
        {
            return BadRequest(
                new
                {
                    mensaje =
                        "El motivo de la regularización no puede superar los 440 caracteres."
                }
            );
        }


        // =================================================
        // FECHA
        // =================================================

        var ahora =
            _fechaHoraService
                .AhoraEcuador();


        var hoy =
            DateOnly
                .FromDateTime(
                    ahora
                );


        if (
            dto.Fecha >
            hoy
        )
        {
            return BadRequest(
                new
                {
                    mensaje =
                        "No es posible regularizar marcaciones en una fecha futura."
                }
            );
        }


        // =================================================
        // EMPLEADO + JORNADA
        //
        // No exigimos que el empleado esté actualmente
        // activo porque RRHH podría regularizar una jornada
        // histórica de un empleado ya inactivo.
        // =================================================

        var empleado =
            await _context
                .Empleado
                .AsNoTracking()
                .Include(e =>
                    e.IdJornadaNavigation)
                .FirstOrDefaultAsync(
                    e =>
                        e.IdEmpleado ==
                        dto.IdEmpleado
                );


        if (
            empleado is null
        )
        {
            return NotFound(
                new
                {
                    mensaje =
                        "Empleado no encontrado."
                }
            );
        }


        // =================================================
        // MARCACIONES ACTIVAS DEL DÍA
        // =================================================

        var marcacionesActivas =
            await _context
                .MarcacionAsistencia
                .AsNoTracking()
                .Where(
                    m =>
                        m.IdEmpleado ==
                            dto.IdEmpleado &&

                        m.FechaMarcacion ==
                            dto.Fecha &&

                        m.EstadoMarcacion ==
                            "Activa"
                )
                .OrderBy(m =>
                    m.FechaHora)
                .ToListAsync();


        // =================================================
        // DUPLICADO
        // =================================================

        var yaExiste =
            marcacionesActivas
                .Any(
                    m =>
                        m.TipoMarcacion ==
                        dto.TipoMarcacion
                );


        if (
            yaExiste
        )
        {
            return Conflict(
                new
                {
                    mensaje =
                        $"Ya existe una marcación activa de tipo " +
                        $"{ObtenerNombreTipo(dto.TipoMarcacion)} " +
                        "para el empleado en esa fecha."
                }
            );
        }


        // =================================================
        // FECHA / HORA PROPUESTA
        // =================================================

        var fechaHora =
            dto.Fecha
                .ToDateTime(
                    dto.Hora
                );


        // Si se está regularizando el día actual, no
        // permitimos insertar una hora futura.

        if (
            dto.Fecha ==
                hoy &&
            fechaHora >
                ahora
        )
        {
            return BadRequest(
                new
                {
                    mensaje =
                        "La hora de la regularización no puede ser posterior a la hora actual."
                }
            );
        }


        // =================================================
        // VALIDAR SECUENCIA
        // =================================================

        var errorSecuencia =
            ValidarSecuenciaRegularizacion(
                dto.TipoMarcacion,
                fechaHora,
                marcacionesActivas
            );


        if (
            errorSecuencia is not null
        )
        {
            return BadRequest(
                new
                {
                    mensaje =
                        errorSecuencia
                }
            );
        }


        // =================================================
        // CREAR MARCACIÓN
        // =================================================

        var marcacion =
            new MarcacionAsistencia
            {
                IdEmpleado =
                    dto.IdEmpleado,

                FechaMarcacion =
                    dto.Fecha,

                FechaHora =
                    fechaHora,

                TipoMarcacion =
                    dto.TipoMarcacion,

                EstadoMarcacion =
                    "Activa",

                Observacion =
                    $"Regularización administrativa: {motivo}",

                FechaCreacion =
                    ahora
            };


        // =================================================
        // TRANSACCIÓN
        //
        // Marcación + auditoría deben formar una sola
        // operación lógica.
        // =================================================

        var strategy =
            _context.Database
                .CreateExecutionStrategy();


        object? respuesta =
            null;


        try
        {
            await strategy
                .ExecuteAsync(
                    async () =>
                    {
                        await using var transaction =
                            await _context
                                .Database
                                .BeginTransactionAsync();


                        try
                        {
                            _context
                                .MarcacionAsistencia
                                .Add(
                                    marcacion
                                );


                            await _context
                                .SaveChangesAsync();


                            // =================================
                            // AUDITORÍA
                            // =================================

                            await _auditoriaService
                                .RegistrarAsync(
                                    User,

                                    accion:
                                        "REGULARIZAR_MARCACION",

                                    entidad:
                                        "MarcacionAsistencia",

                                    idEntidad:
                                        marcacion.IdMarcacion,

                                    valorAnterior:
                                        null,

                                    valorNuevo:
                                        new
                                        {
                                            marcacion.IdMarcacion,

                                            marcacion.IdEmpleado,

                                            Empleado =
                                                $"{empleado.Nombres} " +
                                                $"{empleado.Apellidos}",

                                            marcacion.FechaMarcacion,

                                            marcacion.FechaHora,

                                            marcacion.TipoMarcacion,

                                            marcacion.EstadoMarcacion,

                                            Origen =
                                                "Regularización administrativa",

                                            Motivo =
                                                motivo
                                        },

                                    ipOrigen:
                                        HttpContext
                                            .Connection
                                            .RemoteIpAddress?
                                            .ToString()
                                );


                            await transaction
                                .CommitAsync();


                            respuesta =
                                new
                                {
                                    idMarcacion =
                                        marcacion.IdMarcacion,

                                    idEmpleado =
                                        marcacion.IdEmpleado,

                                    empleado =
                                        $"{empleado.Nombres} " +
                                        $"{empleado.Apellidos}",

                                    fecha =
                                        marcacion.FechaMarcacion,

                                    hora =
                                        marcacion.FechaHora
                                            .ToString(
                                                "HH:mm:ss"
                                            ),

                                    tipoMarcacion =
                                        marcacion.TipoMarcacion,

                                    estadoMarcacion =
                                        marcacion.EstadoMarcacion,

                                    origen =
                                        "Regularización administrativa",

                                    motivo,

                                    mensaje =
                                        $"{ObtenerNombreTipo(marcacion.TipoMarcacion)} " +
                                        "regularizada correctamente."
                                };
                        }
                        catch
                        {
                            await transaction
                                .RollbackAsync();


                            throw;
                        }
                    }
                );
        }
        catch (
            DbUpdateException
        )
        {
            // Protección adicional ante una carrera:
            // la BD posee restricción de unicidad sobre
            // marcaciones activas.

            return Conflict(
                new
                {
                    mensaje =
                        $"Ya existe una marcación activa de tipo " +
                        $"{ObtenerNombreTipo(dto.TipoMarcacion)} " +
                        "para el empleado en esa fecha."
                }
            );
        }


        return Ok(
            respuesta
        );
    }


    // =====================================================
    // PATCH:
    // api/marcaciones-admin/5/anular
    //
    // Anula una marcación existente.
    // No elimina físicamente la fila.
    // =====================================================

    [HttpPatch("{id:long}/anular")]
    public async Task<ActionResult>
        AnularMarcacion(
            long id,
            [FromBody] AnularMarcacionDto dto)
    {
        if (
            string.IsNullOrWhiteSpace(
                dto.Motivo
            )
        )
        {
            return BadRequest(
                new
                {
                    mensaje =
                        "El motivo de la anulación es obligatorio."
                }
            );
        }


        var marcacion =
            await _context
                .MarcacionAsistencia
                .FirstOrDefaultAsync(
                    m =>
                        m.IdMarcacion ==
                        id
                );


        if (
            marcacion is null
        )
        {
            return NotFound(
                new
                {
                    mensaje =
                        "Marcación no encontrada."
                }
            );
        }


        if (
            marcacion.EstadoMarcacion ==
            "Anulada"
        )
        {
            return Conflict(
                new
                {
                    mensaje =
                        "La marcación ya se encuentra anulada."
                }
            );
        }


        var valorAnterior =
            new
            {
                marcacion.IdMarcacion,
                marcacion.IdEmpleado,
                marcacion.FechaMarcacion,
                marcacion.FechaHora,
                marcacion.TipoMarcacion,
                marcacion.EstadoMarcacion,
                marcacion.Observacion
            };


        marcacion.EstadoMarcacion =
            "Anulada";


        marcacion.Observacion =
            dto.Motivo
                .Trim();


        marcacion.FechaModificacion =
            _fechaHoraService
                .AhoraEcuador();


        await _context
            .SaveChangesAsync();


        await _auditoriaService
            .RegistrarAsync(
                User,

                accion:
                    "ANULAR_MARCACION",

                entidad:
                    "MarcacionAsistencia",

                idEntidad:
                    marcacion.IdMarcacion,

                valorAnterior:
                    valorAnterior,

                valorNuevo:
                    new
                    {
                        marcacion.IdMarcacion,
                        marcacion.IdEmpleado,
                        marcacion.FechaMarcacion,
                        marcacion.FechaHora,
                        marcacion.TipoMarcacion,
                        marcacion.EstadoMarcacion,
                        marcacion.Observacion
                    },

                ipOrigen:
                    HttpContext
                        .Connection
                        .RemoteIpAddress?
                        .ToString()
            );


        return Ok(
            new
            {
                mensaje =
                    "Marcación anulada correctamente."
            }
        );
    }


    // =====================================================
    // GET:
    // api/marcaciones-admin/empleado/1
    // ?fecha=2026-09-07
    //
    // Incluye activas y anuladas.
    // =====================================================

    [HttpGet("empleado/{idEmpleado:long}")]
    public async Task<ActionResult>
        GetMarcacionesEmpleado(
            long idEmpleado,
            [FromQuery] DateOnly fecha)
    {
        var empleado =
            await _context
                .Empleado
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    e =>
                        e.IdEmpleado ==
                        idEmpleado
                );


        if (
            empleado is null
        )
        {
            return NotFound(
                new
                {
                    mensaje =
                        "Empleado no encontrado."
                }
            );
        }


        var marcaciones =
            await _context
                .MarcacionAsistencia
                .AsNoTracking()
                .Where(
                    m =>
                        m.IdEmpleado ==
                            idEmpleado &&

                        m.FechaMarcacion ==
                            fecha
                )
                .OrderBy(m =>
                    m.FechaHora)
                .Select(
                    m =>
                        new
                        {
                            m.IdMarcacion,

                            m.IdEmpleado,

                            m.FechaMarcacion,

                            m.FechaHora,

                            m.TipoMarcacion,

                            m.EstadoMarcacion,

                            m.Observacion,

                            m.FechaCreacion,

                            m.FechaModificacion,

                            esFacial =
                                m.ValidacionFacial !=
                                null,

                            origen =
                                m.ValidacionFacial !=
                                null
                                    ? "Facial"
                                    : (
                                        m.Observacion != null &&
                                        m.Observacion.StartsWith(
                                            "Regularización administrativa:"
                                        )
                                            ? "Regularización"
                                            : "Manual"
                                    )
                        }
                )
                .ToListAsync();


        return Ok(
            marcaciones
        );
    }


    // =====================================================
    // GET:
    // api/marcaciones-admin/consulta
    // ?fecha=2026-09-07
    //
    // Consulta administrativa de todas las marcaciones
    // de una fecha.
    //
    // Incluye:
    // - activas
    // - anuladas
    // - facial
    // - manual
    // - regularización
    // =====================================================

    [HttpGet("consulta")]
    public async Task<ActionResult>
        ConsultarMarcaciones(
            [FromQuery] DateOnly? fecha)
    {
        var fechaConsulta =
            fecha ??
            DateOnly.FromDateTime(
                _fechaHoraService
                    .AhoraEcuador()
            );


        var marcaciones =
            await _context
                .MarcacionAsistencia
                .AsNoTracking()
                .Include(m =>
                    m.IdEmpleadoNavigation)
                .Include(m =>
                    m.ValidacionFacial)
                .Where(
                    m =>
                        m.FechaMarcacion ==
                        fechaConsulta
                )
                .OrderBy(m =>
                    m.IdEmpleadoNavigation
                        .Apellidos)
                .ThenBy(m =>
                    m.IdEmpleadoNavigation
                        .Nombres)
                .ThenBy(m =>
                    m.FechaHora)
                .Select(
                    m =>
                        new
                        {
                            idMarcacion =
                                m.IdMarcacion,

                            idEmpleado =
                                m.IdEmpleado,

                            identificacion =
                                m.IdEmpleadoNavigation
                                    .Identificacion,

                            empleado =
                                m.IdEmpleadoNavigation
                                    .Nombres +
                                " " +
                                m.IdEmpleadoNavigation
                                    .Apellidos,

                            area =
                                m.IdEmpleadoNavigation
                                    .Area,

                            cargo =
                                m.IdEmpleadoNavigation
                                    .Cargo,

                            fechaMarcacion =
                                m.FechaMarcacion,

                            fechaHora =
                                m.FechaHora,

                            tipoMarcacion =
                                m.TipoMarcacion,

                            estadoMarcacion =
                                m.EstadoMarcacion,

                            observacion =
                                m.Observacion,

                            esFacial =
                                m.ValidacionFacial !=
                                null,

                            origen =
                                m.ValidacionFacial !=
                                null
                                    ? "Facial"
                                    : (
                                        m.Observacion != null &&
                                        m.Observacion.StartsWith(
                                            "Regularización administrativa:"
                                        )
                                            ? "Regularización"
                                            : "Manual"
                                    ),

                            similitud =
                                m.ValidacionFacial !=
                                null
                                    ? m.ValidacionFacial
                                        .Similitud
                                    : null,

                            umbral =
                                m.ValidacionFacial !=
                                null
                                    ? m.ValidacionFacial
                                        .Umbral
                                    : null,

                            aprobadoFacial =
                                m.ValidacionFacial !=
                                null
                                    ? m.ValidacionFacial
                                        .Aprobado
                                    : (bool?)null
                        }
                )
                .ToListAsync();


        return Ok(
            new
            {
                fecha =
                    fechaConsulta,

                total =
                    marcaciones.Count,

                marcaciones
            }
        );
    }


    // =====================================================
    // VALIDAR SECUENCIA DE REGULARIZACIÓN
    //
    // Orden lógico:
    //
    // Entrada
    //      <
    // InicioAlmuerzo
    //      <
    // FinAlmuerzo
    //      <
    // Salida
    //
    // Solo se compara contra marcaciones que realmente
    // existen. No se obliga a inventar las faltantes.
    // =====================================================

    private static string?
        ValidarSecuenciaRegularizacion(
            string tipoMarcacion,
            DateTime fechaHoraNueva,
            List<MarcacionAsistencia>
                marcaciones)
    {
        var entrada =
            marcaciones
                .FirstOrDefault(
                    m =>
                        m.TipoMarcacion ==
                        "Entrada"
                );


        var inicioAlmuerzo =
            marcaciones
                .FirstOrDefault(
                    m =>
                        m.TipoMarcacion ==
                        "InicioAlmuerzo"
                );


        var finAlmuerzo =
            marcaciones
                .FirstOrDefault(
                    m =>
                        m.TipoMarcacion ==
                        "FinAlmuerzo"
                );


        var salida =
            marcaciones
                .FirstOrDefault(
                    m =>
                        m.TipoMarcacion ==
                        "Salida"
                );


        switch (
            tipoMarcacion
        )
        {
            // =============================================
            // ENTRADA
            // =============================================

            case "Entrada":

                if (
                    inicioAlmuerzo is not null &&
                    fechaHoraNueva >=
                    inicioAlmuerzo.FechaHora
                )
                {
                    return
                        "La hora de entrada debe ser anterior al inicio de almuerzo registrado.";
                }


                if (
                    finAlmuerzo is not null &&
                    fechaHoraNueva >=
                    finAlmuerzo.FechaHora
                )
                {
                    return
                        "La hora de entrada debe ser anterior al fin de almuerzo registrado.";
                }


                if (
                    salida is not null &&
                    fechaHoraNueva >=
                    salida.FechaHora
                )
                {
                    return
                        "La hora de entrada debe ser anterior a la salida registrada.";
                }


                break;


            // =============================================
            // INICIO ALMUERZO
            // =============================================

            case "InicioAlmuerzo":

                if (
                    entrada is not null &&
                    fechaHoraNueva <=
                    entrada.FechaHora
                )
                {
                    return
                        "La hora de inicio de almuerzo debe ser posterior a la entrada.";
                }


                if (
                    finAlmuerzo is not null &&
                    fechaHoraNueva >=
                    finAlmuerzo.FechaHora
                )
                {
                    return
                        "La hora de inicio de almuerzo debe ser anterior al fin de almuerzo.";
                }


                if (
                    salida is not null &&
                    fechaHoraNueva >=
                    salida.FechaHora
                )
                {
                    return
                        "La hora de inicio de almuerzo debe ser anterior a la salida.";
                }


                break;


            // =============================================
            // FIN ALMUERZO
            // =============================================

            case "FinAlmuerzo":

                if (
                    entrada is not null &&
                    fechaHoraNueva <=
                    entrada.FechaHora
                )
                {
                    return
                        "La hora de fin de almuerzo debe ser posterior a la entrada.";
                }


                if (
                    inicioAlmuerzo is not null &&
                    fechaHoraNueva <=
                    inicioAlmuerzo.FechaHora
                )
                {
                    return
                        "La hora de fin de almuerzo debe ser posterior al inicio de almuerzo.";
                }


                if (
                    salida is not null &&
                    fechaHoraNueva >=
                    salida.FechaHora
                )
                {
                    return
                        "La hora de fin de almuerzo debe ser anterior a la salida.";
                }


                break;


            // =============================================
            // SALIDA
            // =============================================

            case "Salida":

                if (
                    entrada is not null &&
                    fechaHoraNueva <=
                    entrada.FechaHora
                )
                {
                    return
                        "La hora de salida debe ser posterior a la entrada.";
                }


                if (
                    inicioAlmuerzo is not null &&
                    fechaHoraNueva <=
                    inicioAlmuerzo.FechaHora
                )
                {
                    return
                        "La hora de salida debe ser posterior al inicio de almuerzo.";
                }


                if (
                    finAlmuerzo is not null &&
                    fechaHoraNueva <=
                    finAlmuerzo.FechaHora
                )
                {
                    return
                        "La hora de salida debe ser posterior al fin de almuerzo.";
                }


                break;
        }


        return null;
    }


    // =====================================================
    // TIPOS PERMITIDOS
    // =====================================================

    private static string[]
        ObtenerTiposPermitidos()
    {
        return
        [
            "Entrada",
            "InicioAlmuerzo",
            "FinAlmuerzo",
            "Salida"
        ];
    }


    // =====================================================
    // NOMBRE AMIGABLE
    // =====================================================

    private static string
        ObtenerNombreTipo(
            string tipo)
    {
        return tipo switch
        {
            "Entrada" =>
                "Entrada",

            "InicioAlmuerzo" =>
                "Inicio de almuerzo",

            "FinAlmuerzo" =>
                "Fin de almuerzo",

            "Salida" =>
                "Salida",

            _ =>
                tipo
        };
    }
}