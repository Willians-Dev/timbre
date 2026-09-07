using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.DTOs.Marcaciones;
using Timbre.Api.Models;
using Timbre.Api.Services;

namespace Timbre.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MarcacionesController : ControllerBase
{
    private readonly TimbreDbContext _context;
    private readonly FechaHoraService _fechaHoraService;
    private readonly UsuarioActualService _usuarioActualService;
    private readonly MarcacionService _marcacionService;

    public MarcacionesController(
        TimbreDbContext context,
        FechaHoraService fechaHoraService,
        UsuarioActualService usuarioActualService,
        MarcacionService marcacionService)
    {
        _context = context;
        _fechaHoraService = fechaHoraService;
        _usuarioActualService = usuarioActualService;
        _marcacionService = marcacionService;
    }

    // =====================================================
    // GET: api/marcaciones/hoy
    //
    // Solo Administrador / RRHH
    // =====================================================
    [Authorize(Roles = "Administrador,RRHH")]
    [HttpGet("hoy")]
    public async Task<ActionResult<IEnumerable<MarcacionDto>>>
        GetMarcacionesHoy()
    {
        var ahora =
            _fechaHoraService.AhoraEcuador();

        var hoy =
            DateOnly.FromDateTime(ahora);

        var marcaciones =
            await _context.MarcacionAsistencia
                .AsNoTracking()
                .Include(m =>
                    m.IdEmpleadoNavigation)
                    .ThenInclude(e =>
                        e.IdJornadaNavigation)
                .Where(m =>
                    m.FechaMarcacion == hoy &&
                    m.EstadoMarcacion == "Activa")
                .OrderBy(m => m.FechaHora)
                .ToListAsync();

        var resultado = marcaciones
            .Select(ConvertirMarcacionDto)
            .ToList();

        return Ok(resultado);
    }

    // =====================================================
    // GET:
    // api/marcaciones/empleado/1/hoy
    //
    // Administrador / RRHH:
    // cualquier empleado
    //
    // Empleado:
    // únicamente él mismo
    // =====================================================
    [HttpGet("empleado/{idEmpleado:long}/hoy")]
    public async Task<ActionResult<IEnumerable<MarcacionDto>>>
        GetMarcacionesEmpleadoHoy(
            long idEmpleado)
    {
        if (!_usuarioActualService.PuedeConsultarEmpleado(
                User,
                idEmpleado))
        {
            return Forbid();
        }

        var ahora =
            _fechaHoraService.AhoraEcuador();

        var hoy =
            DateOnly.FromDateTime(ahora);

        var empleadoExiste =
            await _context.Empleado
                .AsNoTracking()
                .AnyAsync(e =>
                    e.IdEmpleado == idEmpleado);

        if (!empleadoExiste)
        {
            return NotFound(new
            {
                mensaje =
                    "Empleado no encontrado."
            });
        }

        var marcaciones =
            await _context.MarcacionAsistencia
                .AsNoTracking()
                .Include(m =>
                    m.IdEmpleadoNavigation)
                    .ThenInclude(e =>
                        e.IdJornadaNavigation)
                .Where(m =>
                    m.IdEmpleado == idEmpleado &&
                    m.FechaMarcacion == hoy &&
                    m.EstadoMarcacion == "Activa")
                .OrderBy(m => m.FechaHora)
                .ToListAsync();

        var resultado = marcaciones
            .Select(ConvertirMarcacionDto)
            .ToList();

        return Ok(resultado);
    }

    // =====================================================
    // POST: api/marcaciones/registrar
    // =====================================================
    [HttpPost("registrar")]
    public async Task<ActionResult> RegistrarMarcacion(
        [FromBody] RegistrarMarcacionDto dto,
        CancellationToken cancellationToken)
    {
        if (dto.IdEmpleado <= 0)
        {
            return BadRequest(new
            {
                mensaje =
                    "Debe indicar un empleado válido."
            });
        }

        // Administrador / RRHH:
        // pueden registrar para cualquier empleado.
        //
        // Empleado:
        // únicamente para sí mismo.
        if (!_usuarioActualService
            .PuedeConsultarEmpleado(
                User,
                dto.IdEmpleado))
        {
            return Forbid();
        }

        try
        {
            var resultado =
                await _marcacionService
                    .RegistrarAsync(
                        dto.IdEmpleado,
                        cancellationToken
                    );

            return Ok(new
            {
                resultado.IdMarcacion,

                resultado.IdEmpleado,

                resultado.Empleado,

                resultado.Fecha,

                hora =
                    resultado.FechaHora
                        .ToString("HH:mm:ss"),

                resultado.TipoMarcacion,

                resultado.EstadoEntrada,

                resultado.Completa,

                resultado.MarcacionesFaltantes,

                resultado.Mensaje
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                mensaje = ex.Message
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                mensaje = ex.Message
            });
        }
    }

    // =====================================================
    // GET:
    // api/marcaciones/empleado/1/estado-hoy
    // =====================================================
    [HttpGet("empleado/{idEmpleado:long}/estado-hoy")]
    public async Task<ActionResult<EstadoMarcacionDiaDto>>
        GetEstadoHoy(long idEmpleado)
    {
        if (!_usuarioActualService.PuedeConsultarEmpleado(
                User,
                idEmpleado))
        {
            return Forbid();
        }

        var ahora =
            _fechaHoraService.AhoraEcuador();

        var hoy =
            DateOnly.FromDateTime(ahora);

        var empleado =
            await _context.Empleado
                .AsNoTracking()
                .Include(e =>
                    e.IdJornadaNavigation)
                .FirstOrDefaultAsync(e =>
                    e.IdEmpleado == idEmpleado);

        if (empleado is null)
        {
            return NotFound(new
            {
                mensaje =
                    "Empleado no encontrado."
            });
        }

        var marcaciones =
            await _context.MarcacionAsistencia
                .AsNoTracking()
                .Where(m =>
                    m.IdEmpleado == idEmpleado &&
                    m.FechaMarcacion == hoy &&
                    m.EstadoMarcacion == "Activa")
                .OrderBy(m => m.FechaHora)
                .ToListAsync();

        var estado =
            ObtenerEstadoDia(
                empleado.IdEmpleado,
                $"{empleado.Nombres} {empleado.Apellidos}",
                hoy,
                empleado.IdJornadaNavigation,
                marcaciones);

        return Ok(estado);
    }

    // =====================================================
// GET:
// api/marcaciones/mi-asistencia
//
// ?fechaDesde=2026-08-01
// &fechaHasta=2026-08-31
//
// El empleado se obtiene del JWT.
// No se recibe idEmpleado desde el frontend.
// =====================================================
[HttpGet("mi-asistencia")]
    public async Task<ActionResult> GetMiAsistencia(
        [FromQuery] DateOnly? fechaDesde,
        [FromQuery] DateOnly? fechaHasta)
    {
        var claimIdEmpleado =
            User.FindFirst("idEmpleado")?.Value;

        if (!long.TryParse(
            claimIdEmpleado,
            out var idEmpleado))
        {
            return Forbid();
        }


        var ahora =
            _fechaHoraService.AhoraEcuador();

        var hoy =
            DateOnly.FromDateTime(
                ahora
            );


        var desde =
            fechaDesde ??
            new DateOnly(
                hoy.Year,
                hoy.Month,
                1
            );


        var hasta =
            fechaHasta ??
            hoy;


        if (desde > hasta)
        {
            return BadRequest(new
            {
                mensaje =
                    "La fecha desde no puede ser mayor que la fecha hasta."
            });
        }


        if (
            hasta.DayNumber -
            desde.DayNumber >
            366
        )
        {
            return BadRequest(new
            {
                mensaje =
                    "El rango máximo de consulta es de 366 días."
            });
        }


        var empleado =
            await _context.Empleado
                .AsNoTracking()
                .Include(e =>
                    e.IdJornadaNavigation)
                .FirstOrDefaultAsync(e =>
                    e.IdEmpleado ==
                    idEmpleado);


        if (empleado is null)
        {
            return NotFound(new
            {
                mensaje =
                    "Empleado no encontrado."
            });
        }


        var marcaciones =
            await _context.MarcacionAsistencia
                .AsNoTracking()
                .Where(m =>
                    m.IdEmpleado ==
                        idEmpleado &&
                    m.FechaMarcacion >=
                        desde &&
                    m.FechaMarcacion <=
                        hasta)
                .OrderByDescending(m =>
                    m.FechaMarcacion)
                .ThenBy(m =>
                    m.FechaHora)
                .Select(m => new
                {
                    m.IdMarcacion,

                    m.FechaMarcacion,

                    m.FechaHora,

                    m.TipoMarcacion,

                    m.EstadoMarcacion,

                    m.Observacion,

                    esFacial =
                        m.ValidacionFacial !=
                        null
                })
                .ToListAsync();


        var marcacionesActivasHoy =
            marcaciones
                .Where(m =>
                    m.FechaMarcacion ==
                        hoy &&
                    m.EstadoMarcacion ==
                        "Activa")
                .Select(m =>
                    new MarcacionAsistencia
                    {
                        IdMarcacion =
                            m.IdMarcacion,

                        IdEmpleado =
                            empleado.IdEmpleado,

                        FechaMarcacion =
                            m.FechaMarcacion,

                        FechaHora =
                            m.FechaHora,

                        TipoMarcacion =
                            m.TipoMarcacion,

                        EstadoMarcacion =
                            m.EstadoMarcacion,

                        Observacion =
                            m.Observacion
                    })
                .ToList();


        var estadoHoy =
            ObtenerEstadoDia(
                empleado.IdEmpleado,

                $"{empleado.Nombres} {empleado.Apellidos}",

                hoy,

                empleado.IdJornadaNavigation,

                marcacionesActivasHoy
            );


        var dias =
            marcaciones
                .GroupBy(m =>
                    m.FechaMarcacion)
                .OrderByDescending(g =>
                    g.Key)
                .Select(g =>
                {
                    var activas =
                        g
                            .Where(m =>
                                m.EstadoMarcacion ==
                                "Activa")
                            .ToList();


                    var entrada =
                        activas
                            .FirstOrDefault(m =>
                                m.TipoMarcacion ==
                                "Entrada");


                    string? estadoEntrada =
                        null;


                    int minutosAtraso =
                        0;


                    if (entrada is not null)
                    {
                        var estado =
                            CalcularEstadoEntrada(
                                empleado
                                    .IdJornadaNavigation,

                                entrada
                                    .FechaHora
                            );


                        estadoEntrada =
                            estado.Estado;


                        minutosAtraso =
                            estado.MinutosAtraso;
                    }


                    var tieneEntrada =
                        activas.Any(m =>
                            m.TipoMarcacion ==
                            "Entrada");


                    var tieneInicioAlmuerzo =
                        activas.Any(m =>
                            m.TipoMarcacion ==
                            "InicioAlmuerzo");


                    var tieneFinAlmuerzo =
                        activas.Any(m =>
                            m.TipoMarcacion ==
                            "FinAlmuerzo");


                    var tieneSalida =
                        activas.Any(m =>
                            m.TipoMarcacion ==
                            "Salida");


                    return new
                    {
                        fecha =
                            g.Key,

                        estadoEntrada,

                        minutosAtraso,

                        completa =
                            tieneEntrada &&
                            tieneInicioAlmuerzo &&
                            tieneFinAlmuerzo &&
                            tieneSalida,

                        marcaciones =
                            g.Select(m =>
                                new
                                {
                                    m.IdMarcacion,

                                    hora =
                                        m.FechaHora
                                            .ToString(
                                                "HH:mm:ss"
                                            ),

                                    m.TipoMarcacion,

                                    m.EstadoMarcacion,

                                    m.Observacion,

                                    m.esFacial
                                })
                            .ToList()
                    };
                })
                .ToList();


        return Ok(new
        {
            empleado =
                new
                {
                    empleado.IdEmpleado,

                    identificacion =
                        empleado.Identificacion,

                    nombre =
                        $"{empleado.Nombres} {empleado.Apellidos}",

                    empleado.Area,

                    empleado.Cargo
                },

            jornada =
                new
                {
                    nombre =
                        empleado
                            .IdJornadaNavigation
                            .Nombre,

                    horaEntrada =
                        empleado
                            .IdJornadaNavigation
                            .HoraEntrada,

                    horaInicioAlmuerzo =
                        empleado
                            .IdJornadaNavigation
                            .HoraInicioAlmuerzo,

                    horaFinAlmuerzo =
                        empleado
                            .IdJornadaNavigation
                            .HoraFinAlmuerzo,

                    horaSalida =
                        empleado
                            .IdJornadaNavigation
                            .HoraSalida,

                    toleranciaEntradaMinutos =
                        empleado
                            .IdJornadaNavigation
                            .ToleranciaEntradaMinutos
                },

            estadoHoy,

            fechaDesde =
                desde,

            fechaHasta =
                hasta,

            dias
        });
    }

    private static string? DeterminarTipoMarcacion(
        DateTime ahora,
        JornadaLaboral jornada,
        List<MarcacionAsistencia> marcaciones)
    {
        var horaActual =
            TimeOnly.FromDateTime(ahora);

        var tieneEntrada =
            marcaciones.Any(m =>
                m.TipoMarcacion == "Entrada");

        var tieneInicioAlmuerzo =
            marcaciones.Any(m =>
                m.TipoMarcacion == "InicioAlmuerzo");

        var tieneFinAlmuerzo =
            marcaciones.Any(m =>
                m.TipoMarcacion == "FinAlmuerzo");

        var tieneSalida =
            marcaciones.Any(m =>
                m.TipoMarcacion == "Salida");

        if (horaActual >=
            jornada.HoraSalida)
        {
            return !tieneSalida
                ? "Salida"
                : null;
        }

        if (horaActual >=
            jornada.HoraFinAlmuerzo)
        {
            if (tieneInicioAlmuerzo &&
                !tieneFinAlmuerzo)
            {
                return "FinAlmuerzo";
            }

            return null;
        }

        if (horaActual >=
            jornada.HoraInicioAlmuerzo)
        {
            if (tieneEntrada &&
                !tieneInicioAlmuerzo)
            {
                return "InicioAlmuerzo";
            }

            return null;
        }

        if (!tieneEntrada)
        {
            return "Entrada";
        }

        return null;
    }

    private static EstadoMarcacionDiaDto ObtenerEstadoDia(
        long idEmpleado,
        string nombreEmpleado,
        DateOnly fecha,
        JornadaLaboral jornada,
        List<MarcacionAsistencia> marcaciones)
    {
        var entrada = marcaciones
            .FirstOrDefault(m =>
                m.TipoMarcacion == "Entrada" &&
                m.EstadoMarcacion == "Activa");

        var tieneEntrada =
            entrada is not null;

        var tieneInicioAlmuerzo =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                    "InicioAlmuerzo" &&
                m.EstadoMarcacion ==
                    "Activa");

        var tieneFinAlmuerzo =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                    "FinAlmuerzo" &&
                m.EstadoMarcacion ==
                    "Activa");

        var tieneSalida =
            marcaciones.Any(m =>
                m.TipoMarcacion ==
                    "Salida" &&
                m.EstadoMarcacion ==
                    "Activa");

        EstadoEntradaDto? estadoEntrada =
            null;

        if (entrada is not null)
        {
            estadoEntrada =
                CalcularEstadoEntrada(
                    jornada,
                    entrada.FechaHora);
        }

        var faltantes =
            new List<string>();

        if (!tieneEntrada)
            faltantes.Add("Entrada");

        if (!tieneInicioAlmuerzo)
            faltantes.Add("InicioAlmuerzo");

        if (!tieneFinAlmuerzo)
            faltantes.Add("FinAlmuerzo");

        if (!tieneSalida)
            faltantes.Add("Salida");

        return new EstadoMarcacionDiaDto
        {
            IdEmpleado =
                idEmpleado,

            Empleado =
                nombreEmpleado,

            Fecha =
                fecha,

            TieneEntrada =
                tieneEntrada,

            TieneInicioAlmuerzo =
                tieneInicioAlmuerzo,

            TieneFinAlmuerzo =
                tieneFinAlmuerzo,

            TieneSalida =
                tieneSalida,

            EstadoEntrada =
                estadoEntrada?.Estado,

            MinutosAtraso =
                estadoEntrada?.MinutosAtraso ?? 0,

            Completa =
                faltantes.Count == 0,

            MarcacionesFaltantes =
                faltantes
        };
    }

    private static EstadoEntradaDto CalcularEstadoEntrada(
        JornadaLaboral jornada,
        DateTime fechaHoraMarcacion)
    {
        var horaMarcacion =
            TimeOnly.FromDateTime(
                fechaHoraMarcacion);

        var horaLimite =
            jornada.HoraEntrada.AddMinutes(
                jornada.ToleranciaEntradaMinutos);

        if (horaMarcacion <= horaLimite)
        {
            return new EstadoEntradaDto
            {
                Estado = "Puntual",

                MinutosAtraso = 0,

                HoraEntradaProgramada =
                    jornada.HoraEntrada,

                ToleranciaMinutos =
                    jornada.ToleranciaEntradaMinutos,

                HoraLimitePuntual =
                    horaLimite,

                HoraMarcacion =
                    horaMarcacion
            };
        }

        var diferencia =
            horaMarcacion.ToTimeSpan() -
            horaLimite.ToTimeSpan();

        return new EstadoEntradaDto
        {
            Estado = "Atraso",

            MinutosAtraso =
                (int)Math.Ceiling(
                    diferencia.TotalMinutes),

            HoraEntradaProgramada =
                jornada.HoraEntrada,

            ToleranciaMinutos =
                jornada.ToleranciaEntradaMinutos,

            HoraLimitePuntual =
                horaLimite,

            HoraMarcacion =
                horaMarcacion
        };
    }

    private static MarcacionDto ConvertirMarcacionDto(
        MarcacionAsistencia marcacion)
    {
        EstadoEntradaDto? estadoEntrada =
            null;

        if (marcacion.TipoMarcacion ==
            "Entrada")
        {
            estadoEntrada =
                CalcularEstadoEntrada(
                    marcacion
                        .IdEmpleadoNavigation
                        .IdJornadaNavigation,
                    marcacion.FechaHora);
        }

        return new MarcacionDto
        {
            IdMarcacion =
                marcacion.IdMarcacion,

            IdEmpleado =
                marcacion.IdEmpleado,

            Empleado =
                $"{marcacion.IdEmpleadoNavigation.Nombres} " +
                $"{marcacion.IdEmpleadoNavigation.Apellidos}",

            FechaMarcacion =
                marcacion.FechaMarcacion,

            FechaHora =
                marcacion.FechaHora,

            TipoMarcacion =
                marcacion.TipoMarcacion,

            EstadoMarcacion =
                marcacion.EstadoMarcacion,

            Observacion =
                marcacion.Observacion,

            EstadoEntrada =
                estadoEntrada?.Estado,

            MinutosAtraso =
                estadoEntrada?.MinutosAtraso
        };
    }

    private static bool TrabajaHoy(
        JornadaLaboral jornada,
        DayOfWeek dia)
    {
        return dia switch
        {
            DayOfWeek.Monday => jornada.Lunes,
            DayOfWeek.Tuesday => jornada.Martes,
            DayOfWeek.Wednesday => jornada.Miercoles,
            DayOfWeek.Thursday => jornada.Jueves,
            DayOfWeek.Friday => jornada.Viernes,
            DayOfWeek.Saturday => jornada.Sabado,
            DayOfWeek.Sunday => jornada.Domingo,
            _ => false
        };
    }
}