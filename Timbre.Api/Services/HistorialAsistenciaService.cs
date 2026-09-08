using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.DTOs.Historial;

namespace Timbre.Api.Services;


public class HistorialAsistenciaService
{
    private readonly TimbreDbContext
        _context;


    public HistorialAsistenciaService(
        TimbreDbContext context)
    {
        _context =
            context;
    }


    // =====================================================
    // CONSULTAR
    // =====================================================

    public async Task<HistorialAsistenciaResultadoDto>
        ConsultarAsync(
            HistorialAsistenciaFiltroDto filtro,
            CancellationToken cancellationToken =
                default)
    {
        ValidarFiltro(
            filtro
        );


        // =================================================
        // CONSULTA BASE
        // =================================================

        var query =
            _context
                .MarcacionAsistencia
                .AsNoTracking()
                .Include(m =>
                    m.IdEmpleadoNavigation)
                    .ThenInclude(e =>
                        e.IdJornadaNavigation)
                .Include(m =>
                    m.ValidacionFacial)
                .Where(m =>
                    m.FechaMarcacion >=
                        filtro.FechaDesde &&
                    m.FechaMarcacion <=
                        filtro.FechaHasta);


        // =================================================
        // EMPLEADO
        // =================================================

        if (
            filtro.IdEmpleado
                .HasValue
        )
        {
            query =
                query.Where(m =>
                    m.IdEmpleado ==
                    filtro
                        .IdEmpleado
                        .Value
                );
        }


        // =================================================
        // ÁREA
        // =================================================

        if (
            !string.IsNullOrWhiteSpace(
                filtro.Area
            )
        )
        {
            var area =
                filtro.Area
                    .Trim();


            query =
                query.Where(m =>
                    m.IdEmpleadoNavigation
                        .Area ==
                    area
                );
        }


        // =================================================
        // OBTENER MARCACIONES
        // =================================================

        var marcaciones =
            await query
                .OrderBy(m =>
                    m.IdEmpleadoNavigation
                        .Apellidos)
                .ThenBy(m =>
                    m.IdEmpleadoNavigation
                        .Nombres)
                .ThenBy(m =>
                    m.FechaMarcacion)
                .ThenBy(m =>
                    m.FechaHora)
                .ToListAsync(
                    cancellationToken
                );


        // =================================================
        // CONSOLIDACIÓN
        // =================================================

        var registros =
            marcaciones
                .GroupBy(m =>
                    new
                    {
                        m.IdEmpleado,
                        m.FechaMarcacion
                    })
                .Select(
                    grupo =>
                    {
                        var primera =
                            grupo.First();


                        var empleado =
                            primera
                                .IdEmpleadoNavigation;


                        var jornada =
                            empleado
                                .IdJornadaNavigation;


                        var activas =
                            grupo
                                .Where(m =>
                                    m.EstadoMarcacion ==
                                    "Activa"
                                )
                                .OrderBy(m =>
                                    m.FechaHora)
                                .ToList();


                        // =================================
                        // TIPOS
                        // =================================

                        var entrada =
                            activas.FirstOrDefault(
                                m =>
                                    m.TipoMarcacion ==
                                    "Entrada"
                            );


                        var inicioAlmuerzo =
                            activas.FirstOrDefault(
                                m =>
                                    m.TipoMarcacion ==
                                    "InicioAlmuerzo"
                            );


                        var finAlmuerzo =
                            activas.FirstOrDefault(
                                m =>
                                    m.TipoMarcacion ==
                                    "FinAlmuerzo"
                            );


                        var salida =
                            activas.FirstOrDefault(
                                m =>
                                    m.TipoMarcacion ==
                                    "Salida"
                            );


                        // =================================
                        // JORNADA COMPLETA
                        // =================================

                        var completa =
                            entrada is not null &&
                            inicioAlmuerzo is not null &&
                            finAlmuerzo is not null &&
                            salida is not null;


                        // =================================
                        // ATRASO
                        // =================================

                        string? estadoEntrada =
                            null;


                        var minutosAtraso =
                            0;


                        if (
                            entrada is not null &&
                            jornada is not null
                        )
                        {
                            var limite =
                                jornada
                                    .HoraEntrada
                                    .AddMinutes(
                                        jornada
                                            .ToleranciaEntradaMinutos
                                    );


                            var horaEntrada =
                                TimeOnly
                                    .FromDateTime(
                                        entrada
                                            .FechaHora
                                    );


                            if (
                                horaEntrada <=
                                limite
                            )
                            {
                                estadoEntrada =
                                    "Puntual";
                            }
                            else
                            {
                                estadoEntrada =
                                    "Atraso";


                                minutosAtraso =
                                    Math.Max(
                                        0,

                                        (int)Math.Floor(
                                            (
                                                horaEntrada
                                                    .ToTimeSpan() -
                                                limite
                                                    .ToTimeSpan()
                                            )
                                            .TotalMinutes
                                        )
                                    );
                            }
                        }


                        // =================================
                        // ORIGEN
                        // =================================

                        var tieneFacial =
                            activas.Any(m =>
                                m.ValidacionFacial !=
                                null
                            );


                        var tieneManual =
                            activas.Any(m =>
                                m.ValidacionFacial ==
                                null
                            );


                        string origen;


                        if (
                            tieneFacial &&
                            tieneManual
                        )
                        {
                            origen =
                                "Mixto";
                        }
                        else if (
                            tieneFacial
                        )
                        {
                            origen =
                                "Facial";
                        }
                        else
                        {
                            origen =
                                "Manual";
                        }


                        // =================================
                        // DETALLE DE MARCACIONES
                        // Incluimos activas y anuladas.
                        // =================================

                        var detalle =
                            grupo
                                .OrderBy(m =>
                                    m.FechaHora)
                                .Select(m =>
                                    new HistorialMarcacionDto
                                    {
                                        IdMarcacion =
                                            m.IdMarcacion,

                                        Hora =
                                            m.FechaHora
                                                .ToString(
                                                    "HH:mm:ss"
                                                ),

                                        TipoMarcacion =
                                            m.TipoMarcacion,

                                        EstadoMarcacion =
                                            m.EstadoMarcacion,

                                        Observacion =
                                            m.Observacion,

                                        EsFacial =
                                            m.ValidacionFacial !=
                                            null
                                    }
                                )
                                .ToList();


                        return new HistorialRegistroDto
                        {
                            IdEmpleado =
                                empleado
                                    .IdEmpleado,

                            Identificacion =
                                empleado
                                    .Identificacion,

                            Empleado =
                                $"{empleado.Nombres} " +
                                $"{empleado.Apellidos}",

                            Area =
                                empleado.Area,

                            Cargo =
                                empleado.Cargo,

                            Fecha =
                                primera
                                    .FechaMarcacion,

                            Entrada =
                                ObtenerHora(
                                    entrada
                                ),

                            InicioAlmuerzo =
                                ObtenerHora(
                                    inicioAlmuerzo
                                ),

                            FinAlmuerzo =
                                ObtenerHora(
                                    finAlmuerzo
                                ),

                            Salida =
                                ObtenerHora(
                                    salida
                                ),

                            EstadoEntrada =
                                estadoEntrada,

                            MinutosAtraso =
                                minutosAtraso,

                            Completa =
                                completa,

                            EstadoJornada =
                                completa
                                    ? "Completa"
                                    : "Incompleta",

                            TieneFacial =
                                tieneFacial,

                            TieneManual =
                                tieneManual,

                            Origen =
                                origen,

                            Marcaciones =
                                detalle
                        };
                    }
                )
                .ToList();


        // =================================================
        // FILTRO ESTADO JORNADA
        // =================================================

        if (
            !string.IsNullOrWhiteSpace(
                filtro.EstadoJornada
            )
        )
        {
            registros =
                registros
                    .Where(r =>
                        string.Equals(
                            r.EstadoJornada,

                            filtro.EstadoJornada
                                .Trim(),

                            StringComparison
                                .OrdinalIgnoreCase
                        )
                    )
                    .ToList();
        }


        // =================================================
        // FILTRO ESTADO ENTRADA
        // =================================================

        if (
            !string.IsNullOrWhiteSpace(
                filtro.EstadoEntrada
            )
        )
        {
            registros =
                registros
                    .Where(r =>
                        string.Equals(
                            r.EstadoEntrada,

                            filtro.EstadoEntrada
                                .Trim(),

                            StringComparison
                                .OrdinalIgnoreCase
                        )
                    )
                    .ToList();
        }


        // =================================================
        // FILTRO ORIGEN
        // =================================================

        if (
            !string.IsNullOrWhiteSpace(
                filtro.Origen
            )
        )
        {
            registros =
                registros
                    .Where(r =>
                        string.Equals(
                            r.Origen,

                            filtro.Origen
                                .Trim(),

                            StringComparison
                                .OrdinalIgnoreCase
                        )
                    )
                    .ToList();
        }


        // =================================================
        // ORDEN
        // =================================================

        registros =
            registros
                .OrderByDescending(r =>
                    r.Fecha)
                .ThenBy(r =>
                    r.Empleado)
                .ToList();


        return new HistorialAsistenciaResultadoDto
        {
            FechaDesde =
                filtro.FechaDesde,

            FechaHasta =
                filtro.FechaHasta,

            Total =
                registros.Count,

            Registros =
                registros
        };
    }


    // =====================================================
    // FILTROS
    // =====================================================

    public async Task<HistorialFiltrosDto>
        ObtenerFiltrosAsync(
            CancellationToken cancellationToken =
                default)
    {
        var empleados =
            await _context
                .Empleado
                .AsNoTracking()
                .Where(e =>
                    e.Activo)
                .OrderBy(e =>
                    e.Apellidos)
                .ThenBy(e =>
                    e.Nombres)
                .Select(e =>
                    new HistorialEmpleadoFiltroDto
                    {
                        IdEmpleado =
                            e.IdEmpleado,

                        Nombre =
                            e.Nombres +
                            " " +
                            e.Apellidos,

                        Area =
                            e.Area
                    }
                )
                .ToListAsync(
                    cancellationToken
                );


        var areas =
            empleados
                .Where(e =>
                    !string.IsNullOrWhiteSpace(
                        e.Area
                    )
                )
                .Select(e =>
                    e.Area!)
                .Distinct(
                    StringComparer
                        .OrdinalIgnoreCase
                )
                .OrderBy(a =>
                    a)
                .ToList();


        return new HistorialFiltrosDto
        {
            Empleados =
                empleados,

            Areas =
                areas
        };
    }


    // =====================================================
    // VALIDAR FILTRO
    // =====================================================

    private static void ValidarFiltro(
        HistorialAsistenciaFiltroDto filtro)
    {
        if (
            filtro.FechaDesde >
            filtro.FechaHasta
        )
        {
            throw new ArgumentException(
                "La fecha desde no puede ser mayor que la fecha hasta."
            );
        }


        if (
            filtro.FechaHasta.DayNumber -
            filtro.FechaDesde.DayNumber >
            366
        )
        {
            throw new ArgumentException(
                "El rango máximo de consulta es de 366 días."
            );
        }
    }


    // =====================================================
    // OBTENER HORA
    // =====================================================

    private static string? ObtenerHora(
        Models.MarcacionAsistencia? marcacion)
    {
        if (
            marcacion is null
        )
        {
            return null;
        }


        return marcacion
            .FechaHora
            .ToString(
                "HH:mm:ss"
            );
    }
}