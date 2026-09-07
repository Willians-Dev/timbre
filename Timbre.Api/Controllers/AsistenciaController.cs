using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.DTOs.Asistencia;
using Timbre.Api.Models;
using Timbre.Api.Services;

namespace Timbre.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AsistenciaController : ControllerBase
{
    private readonly TimbreDbContext _context;
    private readonly FechaHoraService _fechaHoraService;
    private readonly UsuarioActualService _usuarioActualService;

    public AsistenciaController(
        TimbreDbContext context,
        FechaHoraService fechaHoraService,
        UsuarioActualService usuarioActualService)
    {
        _context = context;
        _fechaHoraService = fechaHoraService;
        _usuarioActualService = usuarioActualService;
    }

    // =====================================================
    // GET: api/asistencia/resumen-diario
    //
    // Solo Administrador / RRHH
    //
    // Opcional:
    // ?fecha=2026-08-24
    // =====================================================
    [Authorize(Roles = "Administrador,RRHH")]
    [HttpGet("resumen-diario")]
    public async Task<ActionResult<IEnumerable<ResumenDiarioAsistenciaDto>>>
        GetResumenDiario([FromQuery] DateOnly? fecha)
    {
        var ahora = _fechaHoraService.AhoraEcuador();

        var fechaConsulta =
            fecha ?? DateOnly.FromDateTime(ahora);

        var empleados = await _context.Empleado
            .AsNoTracking()
            .Include(e => e.IdJornadaNavigation)
            .Where(e => e.Activo)
            .OrderBy(e => e.Apellidos)
            .ThenBy(e => e.Nombres)
            .ToListAsync();

        if (empleados.Count == 0)
        {
            return Ok(
                new List<ResumenDiarioAsistenciaDto>());
        }

        var idsEmpleados = empleados
            .Select(e => e.IdEmpleado)
            .ToList();

        var marcaciones = await _context.MarcacionAsistencia
            .AsNoTracking()
            .Where(m =>
                idsEmpleados.Contains(m.IdEmpleado) &&
                m.FechaMarcacion == fechaConsulta &&
                m.EstadoMarcacion == "Activa")
            .OrderBy(m => m.FechaHora)
            .ToListAsync();

        var marcacionesPorEmpleado = marcaciones
            .GroupBy(m => m.IdEmpleado)
            .ToDictionary(
                g => g.Key,
                g => g.ToList());

        var resultado =
            new List<ResumenDiarioAsistenciaDto>();

        foreach (var empleado in empleados)
        {
            marcacionesPorEmpleado.TryGetValue(
                empleado.IdEmpleado,
                out var marcacionesEmpleado);

            marcacionesEmpleado ??=
                new List<MarcacionAsistencia>();

            resultado.Add(
                ConstruirResumen(
                    empleado,
                    fechaConsulta,
                    marcacionesEmpleado));
        }

        return Ok(resultado);
    }

    // =====================================================
    // GET:
    // api/asistencia/empleado/1/resumen-diario
    //
    // Administrador / RRHH:
    // cualquier empleado
    //
    // Empleado:
    // únicamente él mismo
    // =====================================================
    [HttpGet("empleado/{idEmpleado:long}/resumen-diario")]
    public async Task<ActionResult<ResumenDiarioAsistenciaDto>>
        GetResumenEmpleado(
            long idEmpleado,
            [FromQuery] DateOnly? fecha)
    {
        if (!_usuarioActualService.PuedeConsultarEmpleado(
                User,
                idEmpleado))
        {
            return Forbid();
        }

        var ahora =
            _fechaHoraService.AhoraEcuador();

        var fechaConsulta =
            fecha ?? DateOnly.FromDateTime(ahora);

        var empleado = await _context.Empleado
            .AsNoTracking()
            .Include(e => e.IdJornadaNavigation)
            .FirstOrDefaultAsync(e =>
                e.IdEmpleado == idEmpleado);

        if (empleado is null)
        {
            return NotFound(new
            {
                mensaje = "Empleado no encontrado."
            });
        }

        var marcaciones =
            await _context.MarcacionAsistencia
                .AsNoTracking()
                .Where(m =>
                    m.IdEmpleado == idEmpleado &&
                    m.FechaMarcacion == fechaConsulta &&
                    m.EstadoMarcacion == "Activa")
                .OrderBy(m => m.FechaHora)
                .ToListAsync();

        var resumen = ConstruirResumen(
            empleado,
            fechaConsulta,
            marcaciones);

        return Ok(resumen);
    }

    // =====================================================
    // GET: api/asistencia/historico
    //
    // Solo Administrador / RRHH
    //
    // ?fechaDesde=2026-08-01
    // &fechaHasta=2026-08-31
    // =====================================================
    [Authorize(Roles = "Administrador,RRHH")]
    [HttpGet("historico")]
    public async Task<
        ActionResult<IEnumerable<ResumenHistoricoAsistenciaDto>>>
        GetHistorico(
            [FromQuery] DateOnly fechaDesde,
            [FromQuery] DateOnly fechaHasta)
    {
        var validacion =
            ValidarRangoFechas(
                fechaDesde,
                fechaHasta);

        if (validacion is not null)
        {
            return BadRequest(new
            {
                mensaje = validacion
            });
        }

        var empleados = await _context.Empleado
            .AsNoTracking()
            .Include(e => e.IdJornadaNavigation)
            .Where(e => e.Activo)
            .OrderBy(e => e.Apellidos)
            .ThenBy(e => e.Nombres)
            .ToListAsync();

        if (empleados.Count == 0)
        {
            return Ok(
                new List<ResumenHistoricoAsistenciaDto>());
        }

        var idsEmpleados =
            empleados
                .Select(e => e.IdEmpleado)
                .ToList();

        var marcaciones =
            await _context.MarcacionAsistencia
                .AsNoTracking()
                .Where(m =>
                    idsEmpleados.Contains(m.IdEmpleado) &&
                    m.FechaMarcacion >= fechaDesde &&
                    m.FechaMarcacion <= fechaHasta &&
                    m.EstadoMarcacion == "Activa")
                .OrderBy(m => m.FechaMarcacion)
                .ThenBy(m => m.FechaHora)
                .ToListAsync();

        var marcacionesAgrupadas =
            marcaciones
                .GroupBy(m => new
                {
                    m.IdEmpleado,
                    m.FechaMarcacion
                })
                .ToDictionary(
                    g => (
                        g.Key.IdEmpleado,
                        g.Key.FechaMarcacion),
                    g => g.ToList());

        var resultado =
            new List<ResumenHistoricoAsistenciaDto>();

        foreach (var empleado in empleados)
        {
            var fechaActual = fechaDesde;

            while (fechaActual <= fechaHasta)
            {
                if (TrabajaEnFecha(
                    empleado.IdJornadaNavigation,
                    fechaActual))
                {
                    marcacionesAgrupadas.TryGetValue(
                        (
                            empleado.IdEmpleado,
                            fechaActual
                        ),
                        out var marcacionesDia);

                    marcacionesDia ??=
                        new List<MarcacionAsistencia>();

                    resultado.Add(
                        ConstruirResumenHistorico(
                            empleado,
                            fechaActual,
                            marcacionesDia));
                }

                fechaActual =
                    fechaActual.AddDays(1);
            }
        }

        resultado = resultado
            .OrderBy(r => r.Fecha)
            .ThenBy(r => r.Empleado)
            .ToList();

        return Ok(resultado);
    }

    // =====================================================
    // GET:
    // api/asistencia/empleado/1/historico
    //
    // Administrador / RRHH:
    // cualquier empleado
    //
    // Empleado:
    // únicamente él mismo
    // =====================================================
    [HttpGet("empleado/{idEmpleado:long}/historico")]
    public async Task<
        ActionResult<IEnumerable<ResumenHistoricoAsistenciaDto>>>
        GetHistoricoEmpleado(
            long idEmpleado,
            [FromQuery] DateOnly fechaDesde,
            [FromQuery] DateOnly fechaHasta)
    {
        if (!_usuarioActualService.PuedeConsultarEmpleado(
                User,
                idEmpleado))
        {
            return Forbid();
        }

        var validacion =
            ValidarRangoFechas(
                fechaDesde,
                fechaHasta);

        if (validacion is not null)
        {
            return BadRequest(new
            {
                mensaje = validacion
            });
        }

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
                mensaje = "Empleado no encontrado."
            });
        }

        var marcaciones =
            await _context.MarcacionAsistencia
                .AsNoTracking()
                .Where(m =>
                    m.IdEmpleado == idEmpleado &&
                    m.FechaMarcacion >= fechaDesde &&
                    m.FechaMarcacion <= fechaHasta &&
                    m.EstadoMarcacion == "Activa")
                .OrderBy(m => m.FechaMarcacion)
                .ThenBy(m => m.FechaHora)
                .ToListAsync();

        var marcacionesPorFecha =
            marcaciones
                .GroupBy(m => m.FechaMarcacion)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList());

        var resultado =
            new List<ResumenHistoricoAsistenciaDto>();

        var fechaActual = fechaDesde;

        while (fechaActual <= fechaHasta)
        {
            if (TrabajaEnFecha(
                empleado.IdJornadaNavigation,
                fechaActual))
            {
                marcacionesPorFecha.TryGetValue(
                    fechaActual,
                    out var marcacionesDia);

                marcacionesDia ??=
                    new List<MarcacionAsistencia>();

                resultado.Add(
                    ConstruirResumenHistorico(
                        empleado,
                        fechaActual,
                        marcacionesDia));
            }

            fechaActual =
                fechaActual.AddDays(1);
        }

        return Ok(resultado);
    }

    // =====================================================
    // RESUMEN DIARIO
    // =====================================================
    private static ResumenDiarioAsistenciaDto ConstruirResumen(
        Empleado empleado,
        DateOnly fecha,
        List<MarcacionAsistencia> marcaciones)
    {
        var jornada =
            empleado.IdJornadaNavigation;

        var entrada =
            ObtenerMarcacion(marcaciones, "Entrada");

        var inicioAlmuerzo =
            ObtenerMarcacion(
                marcaciones,
                "InicioAlmuerzo");

        var finAlmuerzo =
            ObtenerMarcacion(
                marcaciones,
                "FinAlmuerzo");

        var salida =
            ObtenerMarcacion(marcaciones, "Salida");

        var faltantes =
            ObtenerMarcacionesFaltantes(
                entrada,
                inicioAlmuerzo,
                finAlmuerzo,
                salida);

        var estadoEntrada =
            CalcularEstadoEntrada(
                jornada,
                entrada);

        var minutosAlmuerzo =
            CalcularMinutosEntreMarcaciones(
                inicioAlmuerzo,
                finAlmuerzo);

        var resultadoSalida =
            CalcularSalidaAnticipada(
                jornada,
                salida);

        var minutosPermanencia =
            CalcularMinutosEntreMarcaciones(
                entrada,
                salida);

        var completa =
            entrada is not null &&
            inicioAlmuerzo is not null &&
            finAlmuerzo is not null &&
            salida is not null;

        var minutosTrabajados =
            CalcularMinutosTrabajados(
                completa,
                minutosPermanencia,
                minutosAlmuerzo);

        var estadoJornada =
            ObtenerEstadoJornada(
                marcaciones.Count,
                completa);

        return new ResumenDiarioAsistenciaDto
        {
            IdEmpleado = empleado.IdEmpleado,

            Empleado =
                $"{empleado.Nombres} {empleado.Apellidos}",

            Area = empleado.Area,

            Cargo = empleado.Cargo,

            Fecha = fecha,

            IdJornada = jornada.IdJornada,

            Jornada = jornada.Nombre,

            HoraEntradaProgramada =
                jornada.HoraEntrada,

            HoraSalidaProgramada =
                jornada.HoraSalida,

            ToleranciaEntradaMinutos =
                jornada.ToleranciaEntradaMinutos,

            Entrada =
                ObtenerHora(entrada),

            InicioAlmuerzo =
                ObtenerHora(inicioAlmuerzo),

            FinAlmuerzo =
                ObtenerHora(finAlmuerzo),

            Salida =
                ObtenerHora(salida),

            EstadoEntrada =
                estadoEntrada.Estado,

            MinutosAtraso =
                estadoEntrada.MinutosAtraso,

            MinutosAlmuerzo =
                minutosAlmuerzo,

            SalidaAnticipada =
                resultadoSalida.SalidaAnticipada,

            MinutosSalidaAnticipada =
                resultadoSalida.MinutosSalidaAnticipada,

            MinutosPermanencia =
                minutosPermanencia,

            MinutosTrabajados =
                minutosTrabajados,

            EstadoJornada =
                estadoJornada,

            Completa =
                completa,

            MarcacionesFaltantes =
                faltantes
        };
    }

    // =====================================================
    // RESUMEN HISTÓRICO
    // =====================================================
    private static ResumenHistoricoAsistenciaDto
        ConstruirResumenHistorico(
            Empleado empleado,
            DateOnly fecha,
            List<MarcacionAsistencia> marcaciones)
    {
        var jornada =
            empleado.IdJornadaNavigation;

        var entrada =
            ObtenerMarcacion(marcaciones, "Entrada");

        var inicioAlmuerzo =
            ObtenerMarcacion(
                marcaciones,
                "InicioAlmuerzo");

        var finAlmuerzo =
            ObtenerMarcacion(
                marcaciones,
                "FinAlmuerzo");

        var salida =
            ObtenerMarcacion(
                marcaciones,
                "Salida");

        var faltantes =
            ObtenerMarcacionesFaltantes(
                entrada,
                inicioAlmuerzo,
                finAlmuerzo,
                salida);

        var estadoEntrada =
            CalcularEstadoEntrada(
                jornada,
                entrada);

        var minutosAlmuerzo =
            CalcularMinutosEntreMarcaciones(
                inicioAlmuerzo,
                finAlmuerzo);

        var resultadoSalida =
            CalcularSalidaAnticipada(
                jornada,
                salida);

        var minutosPermanencia =
            CalcularMinutosEntreMarcaciones(
                entrada,
                salida);

        var completa =
            entrada is not null &&
            inicioAlmuerzo is not null &&
            finAlmuerzo is not null &&
            salida is not null;

        var minutosTrabajados =
            CalcularMinutosTrabajados(
                completa,
                minutosPermanencia,
                minutosAlmuerzo);

        return new ResumenHistoricoAsistenciaDto
        {
            Fecha = fecha,

            IdEmpleado =
                empleado.IdEmpleado,

            Empleado =
                $"{empleado.Nombres} {empleado.Apellidos}",

            Area =
                empleado.Area,

            Cargo =
                empleado.Cargo,

            Jornada =
                jornada.Nombre,

            Entrada =
                ObtenerHora(entrada),

            InicioAlmuerzo =
                ObtenerHora(inicioAlmuerzo),

            FinAlmuerzo =
                ObtenerHora(finAlmuerzo),

            Salida =
                ObtenerHora(salida),

            EstadoEntrada =
                estadoEntrada.Estado,

            MinutosAtraso =
                estadoEntrada.MinutosAtraso,

            MinutosAlmuerzo =
                minutosAlmuerzo,

            SalidaAnticipada =
                resultadoSalida.SalidaAnticipada,

            MinutosSalidaAnticipada =
                resultadoSalida.MinutosSalidaAnticipada,

            MinutosPermanencia =
                minutosPermanencia,

            MinutosTrabajados =
                minutosTrabajados,

            EstadoJornada =
                ObtenerEstadoJornada(
                    marcaciones.Count,
                    completa),

            Completa =
                completa,

            MarcacionesFaltantes =
                faltantes
        };
    }

    private static MarcacionAsistencia? ObtenerMarcacion(
        List<MarcacionAsistencia> marcaciones,
        string tipoMarcacion)
    {
        return marcaciones
            .Where(m =>
                m.TipoMarcacion == tipoMarcacion &&
                m.EstadoMarcacion == "Activa")
            .OrderBy(m => m.FechaHora)
            .FirstOrDefault();
    }

    private static TimeOnly? ObtenerHora(
        MarcacionAsistencia? marcacion)
    {
        if (marcacion is null)
            return null;

        return TimeOnly.FromDateTime(
            marcacion.FechaHora);
    }

    private static List<string> ObtenerMarcacionesFaltantes(
        MarcacionAsistencia? entrada,
        MarcacionAsistencia? inicioAlmuerzo,
        MarcacionAsistencia? finAlmuerzo,
        MarcacionAsistencia? salida)
    {
        var faltantes = new List<string>();

        if (entrada is null)
            faltantes.Add("Entrada");

        if (inicioAlmuerzo is null)
            faltantes.Add("InicioAlmuerzo");

        if (finAlmuerzo is null)
            faltantes.Add("FinAlmuerzo");

        if (salida is null)
            faltantes.Add("Salida");

        return faltantes;
    }

    private static ResultadoEntrada CalcularEstadoEntrada(
        JornadaLaboral jornada,
        MarcacionAsistencia? entrada)
    {
        if (entrada is null)
        {
            return new ResultadoEntrada
            {
                Estado = null,
                MinutosAtraso = 0
            };
        }

        var horaMarcacion =
            TimeOnly.FromDateTime(
                entrada.FechaHora);

        var horaLimite =
            jornada.HoraEntrada.AddMinutes(
                jornada.ToleranciaEntradaMinutos);

        if (horaMarcacion <= horaLimite)
        {
            return new ResultadoEntrada
            {
                Estado = "Puntual",
                MinutosAtraso = 0
            };
        }

        var diferencia =
            horaMarcacion.ToTimeSpan() -
            horaLimite.ToTimeSpan();

        return new ResultadoEntrada
        {
            Estado = "Atraso",

            MinutosAtraso =
                (int)Math.Ceiling(
                    diferencia.TotalMinutes)
        };
    }

    private static int? CalcularMinutosEntreMarcaciones(
        MarcacionAsistencia? inicio,
        MarcacionAsistencia? fin)
    {
        if (inicio is null ||
            fin is null)
        {
            return null;
        }

        var diferencia =
            fin.FechaHora -
            inicio.FechaHora;

        if (diferencia.TotalMinutes < 0)
            return null;

        return (int)Math.Round(
            diferencia.TotalMinutes);
    }

    private static ResultadoSalida CalcularSalidaAnticipada(
        JornadaLaboral jornada,
        MarcacionAsistencia? salida)
    {
        if (salida is null)
        {
            return new ResultadoSalida
            {
                SalidaAnticipada = false,
                MinutosSalidaAnticipada = 0
            };
        }

        var horaSalidaReal =
            TimeOnly.FromDateTime(
                salida.FechaHora);

        if (horaSalidaReal >=
            jornada.HoraSalida)
        {
            return new ResultadoSalida
            {
                SalidaAnticipada = false,
                MinutosSalidaAnticipada = 0
            };
        }

        var diferencia =
            jornada.HoraSalida.ToTimeSpan() -
            horaSalidaReal.ToTimeSpan();

        return new ResultadoSalida
        {
            SalidaAnticipada = true,

            MinutosSalidaAnticipada =
                (int)Math.Ceiling(
                    diferencia.TotalMinutes)
        };
    }

    private static int? CalcularMinutosTrabajados(
        bool completa,
        int? minutosPermanencia,
        int? minutosAlmuerzo)
    {
        if (!completa ||
            !minutosPermanencia.HasValue ||
            !minutosAlmuerzo.HasValue)
        {
            return null;
        }

        var resultado =
            minutosPermanencia.Value -
            minutosAlmuerzo.Value;

        return resultado >= 0
            ? resultado
            : null;
    }

    private static string ObtenerEstadoJornada(
        int cantidadMarcaciones,
        bool completa)
    {
        if (cantidadMarcaciones == 0)
            return "SinMarcaciones";

        if (completa)
            return "Completa";

        return "Incompleta";
    }

    private static string? ValidarRangoFechas(
        DateOnly fechaDesde,
        DateOnly fechaHasta)
    {
        if (fechaDesde > fechaHasta)
        {
            return
                "La fecha desde no puede ser mayor que la fecha hasta.";
        }

        var cantidadDias =
            fechaHasta.DayNumber -
            fechaDesde.DayNumber +
            1;

        if (cantidadDias > 366)
        {
            return
                "El rango máximo permitido es de 366 días.";
        }

        return null;
    }

    private static bool TrabajaEnFecha(
        JornadaLaboral jornada,
        DateOnly fecha)
    {
        return fecha.DayOfWeek switch
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

    private sealed class ResultadoEntrada
    {
        public string? Estado { get; set; }

        public int MinutosAtraso { get; set; }
    }

    private sealed class ResultadoSalida
    {
        public bool SalidaAnticipada { get; set; }

        public int MinutosSalidaAnticipada { get; set; }
    }
}