using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.Services;

namespace Timbre.Api.Controllers;

[Authorize(Roles = "Administrador,RRHH")]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly TimbreDbContext _context;
    private readonly FechaHoraService _fechaHoraService;

    public DashboardController(
        TimbreDbContext context,
        FechaHoraService fechaHoraService)
    {
        _context = context;
        _fechaHoraService = fechaHoraService;
    }


    // =====================================================
    // GET: api/dashboard/resumen
    // =====================================================
    [HttpGet("resumen")]
    public async Task<ActionResult> GetResumen(
        CancellationToken cancellationToken)
    {
        var ahora =
            _fechaHoraService.AhoraEcuador();

        var hoy =
            DateOnly.FromDateTime(
                ahora
            );


        // =================================================
        // EMPLEADOS ACTIVOS
        // =================================================

        var empleadosActivos =
            await _context.Empleado
                .AsNoTracking()
                .CountAsync(
                    e => e.Activo,
                    cancellationToken
                );


        // =================================================
        // ROSTROS ENROLADOS ACTIVOS
        // =================================================

        var rostrosEnrolados =
            await _context.RostroEmpleado
                .AsNoTracking()
                .Where(r =>
                    r.Activo)
                .Select(r =>
                    r.IdEmpleado)
                .Distinct()
                .CountAsync(
                    cancellationToken
                );


        // =================================================
        // MARCACIONES ACTIVAS DE HOY
        // =================================================

        var marcacionesHoy =
            await _context.MarcacionAsistencia
                .AsNoTracking()
                .Include(m =>
                    m.IdEmpleadoNavigation)
                    .ThenInclude(e =>
                        e.IdJornadaNavigation)
                .Include(m =>
                    m.ValidacionFacial)
                .Where(m =>
                    m.FechaMarcacion == hoy &&
                    m.EstadoMarcacion == "Activa")
                .OrderByDescending(m =>
                    m.FechaHora)
                .ToListAsync(
                    cancellationToken
                );


        // =================================================
        // PRESENTES HOY
        //
        // Empleados con al menos una Entrada activa.
        // =================================================

        var presentesHoy =
            marcacionesHoy
                .Where(m =>
                    m.TipoMarcacion ==
                    "Entrada")
                .Select(m =>
                    m.IdEmpleado)
                .Distinct()
                .Count();


        // =================================================
        // ATRASOS
        // =================================================

        var atrasos =
            marcacionesHoy
                .Where(m =>
                    m.TipoMarcacion ==
                    "Entrada")
                .Count(m =>
                {
                    var jornada =
                        m.IdEmpleadoNavigation
                            .IdJornadaNavigation;

                    var horaEntrada =
                        TimeOnly.FromDateTime(
                            m.FechaHora
                        );

                    var horaLimite =
                        jornada.HoraEntrada
                            .AddMinutes(
                                jornada
                                    .ToleranciaEntradaMinutos
                            );

                    return horaEntrada >
                        horaLimite;
                });


        // =================================================
        // MARCACIONES FACIALES / MANUALES
        // =================================================

        var faciales =
            marcacionesHoy
                .Count(m =>
                    m.ValidacionFacial !=
                    null);


        var manuales =
            marcacionesHoy
                .Count(m =>
                    m.ValidacionFacial ==
                    null);


        // =================================================
        // JORNADAS COMPLETAS
        // =================================================

        var jornadasCompletas =
            marcacionesHoy
                .GroupBy(m =>
                    m.IdEmpleado)
                .Count(grupo =>
                {
                    var tipos =
                        grupo
                            .Select(m =>
                                m.TipoMarcacion)
                            .ToHashSet();

                    return
                        tipos.Contains(
                            "Entrada") &&
                        tipos.Contains(
                            "InicioAlmuerzo") &&
                        tipos.Contains(
                            "FinAlmuerzo") &&
                        tipos.Contains(
                            "Salida");
                });


        // =================================================
        // EMPLEADOS SIN MARCACIÓN
        // =================================================

        var empleadosConMarcacion =
            marcacionesHoy
                .Select(m =>
                    m.IdEmpleado)
                .Distinct()
                .Count();


        var sinMarcacion =
            Math.Max(
                empleadosActivos -
                empleadosConMarcacion,
                0
            );


        // =================================================
        // ÚLTIMAS MARCACIONES
        // =================================================

        var ultimasMarcaciones =
            marcacionesHoy
                .Take(8)
                .Select(m =>
                    new
                    {
                        idMarcacion =
                            m.IdMarcacion,

                        idEmpleado =
                            m.IdEmpleado,

                        empleado =
                            m.IdEmpleadoNavigation
                                .Nombres +
                            " " +
                            m.IdEmpleadoNavigation
                                .Apellidos,

                        hora =
                            m.FechaHora
                                .ToString(
                                    "HH:mm:ss"
                                ),

                        tipoMarcacion =
                            m.TipoMarcacion,

                        origen =
                            m.ValidacionFacial !=
                            null
                                ? "Facial"
                                : "Manual",

                        estadoEntrada =
                            m.TipoMarcacion ==
                            "Entrada"
                                ? ObtenerEstadoEntrada(
                                    m
                                )
                                : null
                    })
                .ToList();


        // =================================================
        // RESPUESTA
        // =================================================

        return Ok(
            new
            {
                fecha =
                    hoy,

                horaActual =
                    ahora.ToString(
                        "HH:mm:ss"
                    ),

                empleadosActivos,

                presentesHoy,

                atrasos,

                rostrosEnrolados,

                marcacionesHoy =
                    marcacionesHoy.Count,

                jornadasCompletas,

                sinMarcacion,

                faciales,

                manuales,

                ultimasMarcaciones
            }
        );
    }


    // =====================================================
    // ESTADO ENTRADA
    // =====================================================

    private static string ObtenerEstadoEntrada(
        Timbre.Api.Models.MarcacionAsistencia marcacion)
    {
        var jornada =
            marcacion
                .IdEmpleadoNavigation
                .IdJornadaNavigation;


        var horaMarcacion =
            TimeOnly.FromDateTime(
                marcacion.FechaHora
            );


        var horaLimite =
            jornada
                .HoraEntrada
                .AddMinutes(
                    jornada
                        .ToleranciaEntradaMinutos
                );


        return horaMarcacion <=
            horaLimite
                ? "Puntual"
                : "Atraso";
    }
}