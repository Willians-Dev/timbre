using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Timbre.Api.DTOs.Historial;
using Timbre.Api.Services;

namespace Timbre.Api.Controllers;


[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,RRHH")]
public class ReportesController : ControllerBase
{
    private readonly HistorialAsistenciaService
        _historialService;


    private readonly ReportePdfService
        _reportePdfService;


    public ReportesController(
        HistorialAsistenciaService historialService,
        ReportePdfService reportePdfService)
    {
        _historialService =
            historialService;


        _reportePdfService =
            reportePdfService;
    }


    // =====================================================
    // PDF - ASISTENCIA
    // =====================================================

    [HttpGet("asistencia/pdf")]
    public async Task<IActionResult>
        DescargarAsistenciaPdf(
            [FromQuery] DateOnly fechaDesde,
            [FromQuery] DateOnly fechaHasta,
            [FromQuery] long? idEmpleado,
            [FromQuery] string? area,
            CancellationToken cancellationToken)
    {
        return await GenerarReportePdfAsync(
            tipo:
                "asistencia",

            fechaDesde,
            fechaHasta,
            idEmpleado,
            area,
            origen:
                null,

            cancellationToken
        );
    }


    // =====================================================
    // PDF - ATRASOS
    // =====================================================

    [HttpGet("atrasos/pdf")]
    public async Task<IActionResult>
        DescargarAtrasosPdf(
            [FromQuery] DateOnly fechaDesde,
            [FromQuery] DateOnly fechaHasta,
            [FromQuery] long? idEmpleado,
            [FromQuery] string? area,
            CancellationToken cancellationToken)
    {
        return await GenerarReportePdfAsync(
            tipo:
                "atrasos",

            fechaDesde,
            fechaHasta,
            idEmpleado,
            area,
            origen:
                null,

            cancellationToken
        );
    }


    // =====================================================
    // PDF - JORNADAS INCOMPLETAS
    // =====================================================

    [HttpGet("incompletas/pdf")]
    public async Task<IActionResult>
        DescargarIncompletasPdf(
            [FromQuery] DateOnly fechaDesde,
            [FromQuery] DateOnly fechaHasta,
            [FromQuery] long? idEmpleado,
            [FromQuery] string? area,
            CancellationToken cancellationToken)
    {
        return await GenerarReportePdfAsync(
            tipo:
                "incompletas",

            fechaDesde,
            fechaHasta,
            idEmpleado,
            area,
            origen:
                null,

            cancellationToken
        );
    }


    // =====================================================
    // PDF - MARCACIONES
    // =====================================================

    [HttpGet("marcaciones/pdf")]
    public async Task<IActionResult>
        DescargarMarcacionesPdf(
            [FromQuery] DateOnly fechaDesde,
            [FromQuery] DateOnly fechaHasta,
            [FromQuery] long? idEmpleado,
            [FromQuery] string? area,
            [FromQuery] string? origen,
            CancellationToken cancellationToken)
    {
        return await GenerarReportePdfAsync(
            tipo:
                "marcaciones",

            fechaDesde,
            fechaHasta,
            idEmpleado,
            area,
            origen,

            cancellationToken
        );
    }


    // =====================================================
    // EXCEL - ASISTENCIA
    // =====================================================

    [HttpGet("asistencia/excel")]
    public async Task<IActionResult>
        DescargarAsistenciaExcel(
            [FromQuery] DateOnly fechaDesde,
            [FromQuery] DateOnly fechaHasta,
            [FromQuery] long? idEmpleado,
            [FromQuery] string? area,
            CancellationToken cancellationToken)
    {
        return await GenerarReporteExcelAsync(
            tipo:
                "asistencia",

            fechaDesde,
            fechaHasta,
            idEmpleado,
            area,
            origen:
                null,

            cancellationToken
        );
    }


    // =====================================================
    // EXCEL - ATRASOS
    // =====================================================

    [HttpGet("atrasos/excel")]
    public async Task<IActionResult>
        DescargarAtrasosExcel(
            [FromQuery] DateOnly fechaDesde,
            [FromQuery] DateOnly fechaHasta,
            [FromQuery] long? idEmpleado,
            [FromQuery] string? area,
            CancellationToken cancellationToken)
    {
        return await GenerarReporteExcelAsync(
            tipo:
                "atrasos",

            fechaDesde,
            fechaHasta,
            idEmpleado,
            area,
            origen:
                null,

            cancellationToken
        );
    }


    // =====================================================
    // EXCEL - JORNADAS INCOMPLETAS
    // =====================================================

    [HttpGet("incompletas/excel")]
    public async Task<IActionResult>
        DescargarIncompletasExcel(
            [FromQuery] DateOnly fechaDesde,
            [FromQuery] DateOnly fechaHasta,
            [FromQuery] long? idEmpleado,
            [FromQuery] string? area,
            CancellationToken cancellationToken)
    {
        return await GenerarReporteExcelAsync(
            tipo:
                "incompletas",

            fechaDesde,
            fechaHasta,
            idEmpleado,
            area,
            origen:
                null,

            cancellationToken
        );
    }


    // =====================================================
    // EXCEL - MARCACIONES
    // =====================================================

    [HttpGet("marcaciones/excel")]
    public async Task<IActionResult>
        DescargarMarcacionesExcel(
            [FromQuery] DateOnly fechaDesde,
            [FromQuery] DateOnly fechaHasta,
            [FromQuery] long? idEmpleado,
            [FromQuery] string? area,
            [FromQuery] string? origen,
            CancellationToken cancellationToken)
    {
        return await GenerarReporteExcelAsync(
            tipo:
                "marcaciones",

            fechaDesde,
            fechaHasta,
            idEmpleado,
            area,
            origen,

            cancellationToken
        );
    }


    // =====================================================
    // GENERAR EXCEL
    // =====================================================

    private async Task<IActionResult>
        GenerarReporteExcelAsync(
            string tipo,
            DateOnly fechaDesde,
            DateOnly fechaHasta,
            long? idEmpleado,
            string? area,
            string? origen,
            CancellationToken cancellationToken)
    {
        try
        {
            var filtro =
                CrearFiltro(
                    tipo,
                    fechaDesde,
                    fechaHasta,
                    idEmpleado,
                    area,
                    origen
                );


            var resultado =
                await _historialService
                    .ConsultarAsync(
                        filtro,
                        cancellationToken
                    );


            using var workbook =
                new XLWorkbook();


            if (
                tipo ==
                "marcaciones"
            )
            {
                CrearHojaMarcaciones(
                    workbook,
                    resultado
                );
            }
            else
            {
                CrearHojaJornadas(
                    workbook,
                    resultado,
                    tipo
                );
            }


            using var stream =
                new MemoryStream();


            workbook.SaveAs(
                stream
            );


            var contenido =
                stream.ToArray();


            var nombre =
                ObtenerNombreArchivo(
                    tipo,
                    fechaDesde,
                    fechaHasta,
                    "xlsx"
                );


            return File(
                contenido,

                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                nombre
            );
        }
        catch (
            ArgumentException ex
        )
        {
            return BadRequest(
                new
                {
                    mensaje =
                        ex.Message
                }
            );
        }
    }


    // =====================================================
    // GENERAR PDF
    // =====================================================

    private async Task<IActionResult>
        GenerarReportePdfAsync(
            string tipo,
            DateOnly fechaDesde,
            DateOnly fechaHasta,
            long? idEmpleado,
            string? area,
            string? origen,
            CancellationToken cancellationToken)
    {
        try
        {
            var filtro =
                CrearFiltro(
                    tipo,
                    fechaDesde,
                    fechaHasta,
                    idEmpleado,
                    area,
                    origen
                );


            var resultado =
                await _historialService
                    .ConsultarAsync(
                        filtro,
                        cancellationToken
                    );


            byte[] contenido;


            switch (
                tipo
            )
            {
                case "atrasos":

                    contenido =
                        _reportePdfService
                            .GenerarAtrasos(
                                resultado
                            );

                    break;


                case "incompletas":

                    contenido =
                        _reportePdfService
                            .GenerarIncompletas(
                                resultado
                            );

                    break;


                case "marcaciones":

                    contenido =
                        _reportePdfService
                            .GenerarMarcaciones(
                                resultado
                            );

                    break;


                default:

                    contenido =
                        _reportePdfService
                            .GenerarAsistencia(
                                resultado
                            );

                    break;
            }


            var nombre =
                ObtenerNombreArchivo(
                    tipo,
                    fechaDesde,
                    fechaHasta,
                    "pdf"
                );


            return File(
                contenido,
                "application/pdf",
                nombre
            );
        }
        catch (
            ArgumentException ex
        )
        {
            return BadRequest(
                new
                {
                    mensaje =
                        ex.Message
                }
            );
        }
    }


    // =====================================================
    // CREAR FILTRO COMPARTIDO
    //
    // Evita repetir la misma lógica entre Excel y PDF.
    // =====================================================

    private static HistorialAsistenciaFiltroDto
        CrearFiltro(
            string tipo,
            DateOnly fechaDesde,
            DateOnly fechaHasta,
            long? idEmpleado,
            string? area,
            string? origen)
    {
        var filtro =
            new HistorialAsistenciaFiltroDto
            {
                FechaDesde =
                    fechaDesde,

                FechaHasta =
                    fechaHasta,

                IdEmpleado =
                    idEmpleado,

                Area =
                    area,

                Origen =
                    origen
            };


        switch (
            tipo
        )
        {
            case "atrasos":

                filtro.EstadoEntrada =
                    "Atraso";

                break;


            case "incompletas":

                filtro.EstadoJornada =
                    "Incompleta";

                break;
        }


        return filtro;
    }


    // =====================================================
    // EXCEL - HOJA JORNADAS CONSOLIDADAS
    // =====================================================

    private static void CrearHojaJornadas(
        XLWorkbook workbook,
        HistorialAsistenciaResultadoDto resultado,
        string tipo)
    {
        var nombreHoja =
            tipo switch
            {
                "atrasos" =>
                    "Atrasos",

                "incompletas" =>
                    "Incompletas",

                _ =>
                    "Asistencia"
            };


        var titulo =
            tipo switch
            {
                "atrasos" =>
                    "REPORTE DE ATRASOS",

                "incompletas" =>
                    "REPORTE DE JORNADAS INCOMPLETAS",

                _ =>
                    "REPORTE DE ASISTENCIA"
            };


        var ws =
            workbook.Worksheets
                .Add(
                    nombreHoja
                );


        // =================================================
        // TÍTULO
        // 13 columnas: A:M
        // =================================================

        ws.Cell(
            1,
            1
        ).Value =
            titulo;


        ws.Range(
            1,
            1,
            1,
            13
        )
        .Merge();


        ws.Cell(
            1,
            1
        )
        .Style
        .Font
        .Bold =
            true;


        ws.Cell(
            1,
            1
        )
        .Style
        .Font
        .FontSize =
            16;


        // =================================================
        // PERÍODO
        // =================================================

        ws.Cell(
            2,
            1
        ).Value =
            $"Período: " +
            $"{resultado.FechaDesde:dd/MM/yyyy} - " +
            $"{resultado.FechaHasta:dd/MM/yyyy}";


        ws.Range(
            2,
            1,
            2,
            13
        )
        .Merge();


        // =================================================
        // ENCABEZADOS
        // =================================================

        var encabezados =
            new[]
            {
                "Identificación",
                "Empleado",
                "Área",
                "Cargo",
                "Fecha",
                "Entrada",
                "Inicio almuerzo",
                "Fin almuerzo",
                "Salida",
                "Estado entrada",
                "Min. atraso",
                "Estado jornada",
                "Origen"
            };


        for (
            var columna = 0;
            columna <
            encabezados.Length;
            columna++
        )
        {
            ws.Cell(
                4,
                columna + 1
            ).Value =
                encabezados[
                    columna
                ];
        }


        // =================================================
        // DATOS
        // =================================================

        var fila =
            5;


        foreach (
            var registro
            in resultado.Registros
        )
        {
            ws.Cell(
                fila,
                1
            ).Value =
                registro.Identificacion;


            ws.Cell(
                fila,
                2
            ).Value =
                registro.Empleado;


            ws.Cell(
                fila,
                3
            ).Value =
                registro.Area ??
                string.Empty;


            ws.Cell(
                fila,
                4
            ).Value =
                registro.Cargo ??
                string.Empty;


            ws.Cell(
                fila,
                5
            ).Value =
                registro.Fecha
                    .ToDateTime(
                        TimeOnly.MinValue
                    );


            ws.Cell(
                fila,
                5
            )
            .Style
            .DateFormat
            .Format =
                "dd/mm/yyyy";


            ws.Cell(
                fila,
                6
            ).Value =
                registro.Entrada ??
                string.Empty;


            ws.Cell(
                fila,
                7
            ).Value =
                registro.InicioAlmuerzo ??
                string.Empty;


            ws.Cell(
                fila,
                8
            ).Value =
                registro.FinAlmuerzo ??
                string.Empty;


            ws.Cell(
                fila,
                9
            ).Value =
                registro.Salida ??
                string.Empty;


            ws.Cell(
                fila,
                10
            ).Value =
                registro.EstadoEntrada ??
                "Sin entrada";


            ws.Cell(
                fila,
                11
            ).Value =
                registro.MinutosAtraso;


            ws.Cell(
                fila,
                12
            ).Value =
                registro.EstadoJornada;


            ws.Cell(
                fila,
                13
            ).Value =
                registro.Origen;


            fila++;
        }


        AplicarFormato(
            ws,
            encabezados.Length,
            fila - 1
        );
    }


    // =====================================================
    // EXCEL - HOJA MARCACIONES
    // =====================================================

    private static void CrearHojaMarcaciones(
        XLWorkbook workbook,
        HistorialAsistenciaResultadoDto resultado)
    {
        var ws =
            workbook.Worksheets
                .Add(
                    "Marcaciones"
                );


        // =================================================
        // TÍTULO
        // 9 columnas: A:I
        // =================================================

        ws.Cell(
            1,
            1
        ).Value =
            "REPORTE DE MARCACIONES";


        ws.Range(
            1,
            1,
            1,
            9
        )
        .Merge();


        ws.Cell(
            1,
            1
        )
        .Style
        .Font
        .Bold =
            true;


        ws.Cell(
            1,
            1
        )
        .Style
        .Font
        .FontSize =
            16;


        // =================================================
        // PERÍODO
        // =================================================

        ws.Cell(
            2,
            1
        ).Value =
            $"Período: " +
            $"{resultado.FechaDesde:dd/MM/yyyy} - " +
            $"{resultado.FechaHasta:dd/MM/yyyy}";


        ws.Range(
            2,
            1,
            2,
            9
        )
        .Merge();


        // =================================================
        // ENCABEZADOS
        // =================================================

        var encabezados =
            new[]
            {
                "Identificación",
                "Empleado",
                "Área",
                "Fecha",
                "Hora",
                "Tipo",
                "Estado",
                "Origen",
                "Observación"
            };


        for (
            var columna = 0;
            columna <
            encabezados.Length;
            columna++
        )
        {
            ws.Cell(
                4,
                columna + 1
            ).Value =
                encabezados[
                    columna
                ];
        }


        // =================================================
        // DATOS
        // =================================================

        var fila =
            5;


        foreach (
            var registro
            in resultado.Registros
        )
        {
            foreach (
                var marcacion
                in registro.Marcaciones
            )
            {
                ws.Cell(
                    fila,
                    1
                ).Value =
                    registro.Identificacion;


                ws.Cell(
                    fila,
                    2
                ).Value =
                    registro.Empleado;


                ws.Cell(
                    fila,
                    3
                ).Value =
                    registro.Area ??
                    string.Empty;


                ws.Cell(
                    fila,
                    4
                ).Value =
                    registro.Fecha
                        .ToDateTime(
                            TimeOnly.MinValue
                        );


                ws.Cell(
                    fila,
                    4
                )
                .Style
                .DateFormat
                .Format =
                    "dd/mm/yyyy";


                ws.Cell(
                    fila,
                    5
                ).Value =
                    marcacion.Hora;


                ws.Cell(
                    fila,
                    6
                ).Value =
                    ObtenerNombreTipo(
                        marcacion
                            .TipoMarcacion
                    );


                ws.Cell(
                    fila,
                    7
                ).Value =
                    marcacion
                        .EstadoMarcacion;


                ws.Cell(
                    fila,
                    8
                ).Value =
                    marcacion.EsFacial
                        ? "Facial"
                        : "Manual";


                ws.Cell(
                    fila,
                    9
                ).Value =
                    marcacion.Observacion ??
                    string.Empty;


                fila++;
            }
        }


        AplicarFormato(
            ws,
            encabezados.Length,
            fila - 1
        );
    }


    // =====================================================
    // FORMATO EXCEL
    // =====================================================

    private static void AplicarFormato(
        IXLWorksheet ws,
        int cantidadColumnas,
        int ultimaFila)
    {
        // =================================================
        // TÍTULO
        // =================================================

        ws.Cell(
            1,
            1
        )
        .Style
        .Font
        .FontColor =
            XLColor.FromHtml(
                "#214796"
            );


        ws.Cell(
            1,
            1
        )
        .Style
        .Alignment
        .Vertical =
            XLAlignmentVerticalValues
                .Center;


        ws.Row(
            1
        ).Height =
            25;


        // =================================================
        // PERÍODO
        // =================================================

        ws.Cell(
            2,
            1
        )
        .Style
        .Font
        .FontColor =
            XLColor.FromHtml(
                "#667085"
            );


        // =================================================
        // ENCABEZADO TABLA
        // =================================================

        var encabezado =
            ws.Range(
                4,
                1,
                4,
                cantidadColumnas
            );


        encabezado
            .Style
            .Font
            .Bold =
                true;


        encabezado
            .Style
            .Fill
            .BackgroundColor =
                XLColor.FromHtml(
                    "#214796"
                );


        encabezado
            .Style
            .Font
            .FontColor =
                XLColor.White;


        encabezado
            .Style
            .Alignment
            .Vertical =
                XLAlignmentVerticalValues
                    .Center;


        encabezado
            .Style
            .Alignment
            .Horizontal =
                XLAlignmentHorizontalValues
                    .Center;


        ws.Row(
            4
        ).Height =
            22;


        // =================================================
        // DATOS + AUTOFILTRO
        // =================================================

        if (
            ultimaFila >=
            5
        )
        {
            var rangoDatos =
                ws.Range(
                    4,
                    1,
                    ultimaFila,
                    cantidadColumnas
                );


            rangoDatos
                .Style
                .Border
                .BottomBorder =
                    XLBorderStyleValues
                        .Thin;


            rangoDatos
                .Style
                .Border
                .BottomBorderColor =
                    XLColor.FromHtml(
                        "#E4E7EC"
                    );


            // Autofiltro desde encabezados reales
            // de la fila 4.

            rangoDatos
                .SetAutoFilter();
        }
        else
        {
            // Si no existen registros, dejamos
            // filtro solamente sobre encabezados.

            encabezado
                .SetAutoFilter();
        }


        // =================================================
        // AJUSTAR COLUMNAS
        // =================================================

        ws.Columns(
            1,
            cantidadColumnas
        )
        .AdjustToContents();


        foreach (
            var columna
            in ws.Columns(
                1,
                cantidadColumnas
            )
        )
        {
            if (
                columna.Width >
                45
            )
            {
                columna.Width =
                    45;
            }
        }


        // =================================================
        // CONGELAR FILAS
        //
        // Se mantienen visibles:
        // 1 título
        // 2 período
        // 3 separación
        // 4 encabezados
        // =================================================

        ws.SheetView
            .FreezeRows(
                4
            );
    }


    // =====================================================
    // TIPO MARCACIÓN
    // =====================================================

    private static string ObtenerNombreTipo(
        string tipo)
    {
        return tipo switch
        {
            "InicioAlmuerzo" =>
                "Inicio almuerzo",

            "FinAlmuerzo" =>
                "Fin almuerzo",

            _ =>
                tipo
        };
    }


    // =====================================================
    // NOMBRE ARCHIVO
    // =====================================================

    private static string ObtenerNombreArchivo(
        string tipo,
        DateOnly fechaDesde,
        DateOnly fechaHasta,
        string extension)
    {
        var prefijo =
            tipo switch
            {
                "atrasos" =>
                    "Atrasos",

                "incompletas" =>
                    "Jornadas_Incompletas",

                "marcaciones" =>
                    "Marcaciones",

                _ =>
                    "Asistencia"
            };


        return
            $"{prefijo}_" +
            $"{fechaDesde:yyyyMMdd}_" +
            $"{fechaHasta:yyyyMMdd}." +
            extension;
    }
}