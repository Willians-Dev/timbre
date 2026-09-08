using PdfSharp.Drawing;
using PdfSharp.Pdf;
using Timbre.Api.DTOs.Historial;

namespace Timbre.Api.Services;


public class ReportePdfService
{
    // =====================================================
    // FUENTE PDF
    //
    // WINDOWS:
    // PDFsharp utilizará Arial instalada en el sistema.
    //
    // LINUX / AZURE:
    // PdfFontResolver interceptará esta familia y utilizará
    // Liberation Sans incluida en la aplicación.
    // =====================================================

    private const string FamiliaFuentePdf =
        "Arial";
        
    // =====================================================
    // COLORES
    // =====================================================

    private static readonly XColor AzulPrincipal =
        XColor.FromArgb(
            33,
            71,
            150
        );


    private static readonly XColor GrisTexto =
        XColor.FromArgb(
            71,
            84,
            103
        );


    private static readonly XColor GrisClaro =
        XColor.FromArgb(
            242,
            244,
            247
        );


    private static readonly XColor GrisBorde =
        XColor.FromArgb(
            208,
            213,
            221
        );


    private static readonly XColor Verde =
        XColor.FromArgb(
            8,
            116,
            67
        );


    private static readonly XColor Rojo =
        XColor.FromArgb(
            180,
            35,
            24
        );


    // =====================================================
    // MÁRGENES
    // =====================================================

    private const double MargenIzquierdo =
        28;


    private const double MargenDerecho =
        28;


    private const double MargenSuperior =
        28;


    private const double MargenInferior =
        30;


    // =====================================================
    // ASISTENCIA
    // =====================================================

    public byte[] GenerarAsistencia(
        HistorialAsistenciaResultadoDto resultado)
    {
        return GenerarJornadas(
            resultado,
            "REPORTE DE ASISTENCIA",
            "Consolidado de asistencia por empleado y fecha."
        );
    }


    // =====================================================
    // ATRASOS
    // =====================================================

    public byte[] GenerarAtrasos(
        HistorialAsistenciaResultadoDto resultado)
    {
        return GenerarJornadas(
            resultado,
            "REPORTE DE ATRASOS",
            "Entradas registradas después de la hora permitida."
        );
    }


    // =====================================================
    // JORNADAS INCOMPLETAS
    // =====================================================

    public byte[] GenerarIncompletas(
        HistorialAsistenciaResultadoDto resultado)
    {
        return GenerarJornadas(
            resultado,
            "REPORTE DE JORNADAS INCOMPLETAS",
            "Jornadas que no cuentan con todas las marcaciones requeridas."
        );
    }


    // =====================================================
    // MARCACIONES DETALLADAS
    // =====================================================

    public byte[] GenerarMarcaciones(
        HistorialAsistenciaResultadoDto resultado)
    {
        using var documento =
            new PdfDocument();


        documento.Info.Title =
            "Reporte de marcaciones";


        documento.Info.Subject =
            "Detalle de marcaciones de asistencia";


        PdfPage? pagina =
            null;


        XGraphics? gfx =
            null;


        double y =
            0;


        var numeroPagina =
            0;


        var fontTitulo =
            new XFont(
                FamiliaFuentePdf,
                15,
                XFontStyleEx.Bold
            );

        var fontSubtitulo =
            new XFont(
                FamiliaFuentePdf,
                8,
                XFontStyleEx.Regular
            );

        var fontCabecera =
            new XFont(
                FamiliaFuentePdf,
                6.5,
                XFontStyleEx.Bold
            );

        var fontDato =
            new XFont(
                FamiliaFuentePdf,
                6.3,
                XFontStyleEx.Regular
            );


        var columnas =
            new[]
            {
                new ColumnaPdf(
                    "Identificación",
                    68
                ),

                new ColumnaPdf(
                    "Empleado",
                    125
                ),

                new ColumnaPdf(
                    "Área",
                    85
                ),

                new ColumnaPdf(
                    "Fecha",
                    55
                ),

                new ColumnaPdf(
                    "Hora",
                    42
                ),

                new ColumnaPdf(
                    "Tipo",
                    72
                ),

                new ColumnaPdf(
                    "Estado",
                    52
                ),

                new ColumnaPdf(
                    "Origen",
                    45
                ),

                new ColumnaPdf(
                    "Observación",
                    125
                )
            };


        void NuevaPagina()
        {
            gfx?.Dispose();


            pagina =
                documento.AddPage();


            pagina.Size =
                PdfSharp.PageSize
                    .A4;


            pagina.Orientation =
                PdfSharp.PageOrientation
                    .Landscape;


            gfx =
                XGraphics.FromPdfPage(
                    pagina
                );


            numeroPagina++;


            y =
                DibujarEncabezado(
                    gfx,
                    pagina,
                    "REPORTE DE MARCACIONES",
                    "Detalle individual de marcaciones.",
                    resultado.FechaDesde,
                    resultado.FechaHasta,
                    fontTitulo,
                    fontSubtitulo
                );


            y =
                DibujarCabeceraTabla(
                    gfx,
                    y,
                    columnas,
                    fontCabecera
                );
        }


        NuevaPagina();


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
                if (
                    pagina is null ||
                    gfx is null
                )
                {
                    continue;
                }


                const double altoFila =
                    23;


                if (
                    y +
                    altoFila >
                    pagina.Height.Point -
                    MargenInferior -
                    14
                )
                {
                    DibujarPiePagina(
                        gfx,
                        pagina,
                        numeroPagina,
                        fontSubtitulo
                    );


                    NuevaPagina();
                }


                var valores =
                    new[]
                    {
                        registro.Identificacion,

                        registro.Empleado,

                        registro.Area ??
                        string.Empty,

                        registro.Fecha
                            .ToString(
                                "dd/MM/yyyy"
                            ),

                        marcacion.Hora,

                        ObtenerNombreTipo(
                            marcacion.TipoMarcacion
                        ),

                        marcacion.EstadoMarcacion,

                        marcacion.EsFacial
                            ? "Facial"
                            : "Manual",

                        marcacion.Observacion ??
                        string.Empty
                    };


                DibujarFila(
                    gfx!,
                    y,
                    altoFila,
                    columnas,
                    valores,
                    fontDato
                );


                y +=
                    altoFila;
            }
        }


        if (
            pagina is not null &&
            gfx is not null
        )
        {
            DibujarPiePagina(
                gfx,
                pagina,
                numeroPagina,
                fontSubtitulo
            );


            gfx.Dispose();
        }


        using var stream =
            new MemoryStream();


        documento.Save(
            stream,
            false
        );


        return stream.ToArray();
    }


    // =====================================================
    // GENERADOR JORNADAS CONSOLIDADAS
    // =====================================================

    private byte[] GenerarJornadas(
        HistorialAsistenciaResultadoDto resultado,
        string titulo,
        string descripcion)
    {
        using var documento =
            new PdfDocument();


        documento.Info.Title =
            titulo;


        PdfPage? pagina =
            null;


        XGraphics? gfx =
            null;


        double y =
            0;


        var numeroPagina =
            0;


        var fontTitulo =
            new XFont(
                FamiliaFuentePdf,
                15,
                XFontStyleEx.Bold
            );


        var fontSubtitulo =
            new XFont(
                FamiliaFuentePdf,
                8,
                XFontStyleEx.Regular
            );


        var fontCabecera =
            new XFont(
                FamiliaFuentePdf,
                6.3,
                XFontStyleEx.Bold
            );


        var fontDato =
            new XFont(
                FamiliaFuentePdf,
                6.1,
                XFontStyleEx.Regular
            );


        var columnas =
            new[]
            {
                new ColumnaPdf(
                    "Identificación",
                    65
                ),

                new ColumnaPdf(
                    "Empleado",
                    115
                ),

                new ColumnaPdf(
                    "Área",
                    70
                ),

                new ColumnaPdf(
                    "Fecha",
                    52
                ),

                new ColumnaPdf(
                    "Entrada",
                    42
                ),

                new ColumnaPdf(
                    "Inicio alm.",
                    47
                ),

                new ColumnaPdf(
                    "Fin alm.",
                    47
                ),

                new ColumnaPdf(
                    "Salida",
                    42
                ),

                new ColumnaPdf(
                    "Estado entrada",
                    65
                ),

                new ColumnaPdf(
                    "Atraso",
                    42
                ),

                new ColumnaPdf(
                    "Jornada",
                    62
                ),

                new ColumnaPdf(
                    "Origen",
                    45
                )
            };


        void NuevaPagina()
        {
            gfx?.Dispose();


            pagina =
                documento.AddPage();


            pagina.Orientation =
                PdfSharp.PageOrientation
                    .Landscape;


            pagina.Size =
                PdfSharp.PageSize
                    .A4;


            gfx =
                XGraphics.FromPdfPage(
                    pagina
                );


            numeroPagina++;


            y =
                DibujarEncabezado(
                    gfx,
                    pagina,
                    titulo,
                    descripcion,
                    resultado.FechaDesde,
                    resultado.FechaHasta,
                    fontTitulo,
                    fontSubtitulo
                );


            y =
                DibujarCabeceraTabla(
                    gfx,
                    y,
                    columnas,
                    fontCabecera
                );
        }


        NuevaPagina();


        foreach (
            var registro
            in resultado.Registros
        )
        {
            if (
                pagina is null ||
                gfx is null
            )
            {
                continue;
            }


            const double altoFila =
                22;


            if (
                y +
                altoFila >
                pagina.Height.Point -
                MargenInferior -
                14
            )
            {
                DibujarPiePagina(
                    gfx,
                    pagina,
                    numeroPagina,
                    fontSubtitulo
                );


                NuevaPagina();
            }


            var estadoEntrada =
                registro.EstadoEntrada ??
                "Sin entrada";


            if (
                registro.MinutosAtraso >
                0
            )
            {
                estadoEntrada +=
                    $" ({registro.MinutosAtraso} min)";
            }


            var valores =
                new[]
                {
                    registro.Identificacion,

                    registro.Empleado,

                    registro.Area ??
                    string.Empty,

                    registro.Fecha
                        .ToString(
                            "dd/MM/yyyy"
                        ),

                    FormatearHora(
                        registro.Entrada
                    ),

                    FormatearHora(
                        registro.InicioAlmuerzo
                    ),

                    FormatearHora(
                        registro.FinAlmuerzo
                    ),

                    FormatearHora(
                        registro.Salida
                    ),

                    estadoEntrada,

                    registro.MinutosAtraso > 0
                        ? registro.MinutosAtraso
                            .ToString()
                        : "-",

                    registro.EstadoJornada,

                    registro.Origen
                };


            DibujarFila(
                gfx,
                y,
                altoFila,
                columnas,
                valores,
                fontDato
            );


            y +=
                altoFila;
        }


        if (
            pagina is not null &&
            gfx is not null
        )
        {
            DibujarPiePagina(
                gfx,
                pagina,
                numeroPagina,
                fontSubtitulo
            );


            gfx.Dispose();
        }


        using var stream =
            new MemoryStream();


        documento.Save(
            stream,
            false
        );


        return stream.ToArray();
    }


    // =====================================================
    // ENCABEZADO
    // =====================================================

    private static double DibujarEncabezado(
        XGraphics gfx,
        PdfPage pagina,
        string titulo,
        string descripcion,
        DateOnly fechaDesde,
        DateOnly fechaHasta,
        XFont fontTitulo,
        XFont fontSubtitulo)
    {
        var ancho =
            pagina.Width.Point -
            MargenIzquierdo -
            MargenDerecho;


        // =================================================
        // LÍNEA INSTITUCIONAL
        // =================================================

        gfx.DrawRectangle(
            new XSolidBrush(
                AzulPrincipal
            ),
            MargenIzquierdo,
            MargenSuperior,
            ancho,
            4
        );


        // =================================================
        // TÍTULO
        // =================================================

        gfx.DrawString(
            titulo,
            fontTitulo,
            new XSolidBrush(
                AzulPrincipal
            ),
            new XRect(
                MargenIzquierdo,
                MargenSuperior +
                12,
                ancho,
                22
            ),
            XStringFormats
                .TopLeft
        );


        // =================================================
        // DESCRIPCIÓN
        // =================================================

        gfx.DrawString(
            descripcion,
            fontSubtitulo,
            new XSolidBrush(
                GrisTexto
            ),
            new XRect(
                MargenIzquierdo,
                MargenSuperior +
                36,
                ancho,
                14
            ),
            XStringFormats
                .TopLeft
        );


        // =================================================
        // PERÍODO
        // =================================================

        var periodo =
            $"Período: " +
            $"{fechaDesde:dd/MM/yyyy} - " +
            $"{fechaHasta:dd/MM/yyyy}";


        gfx.DrawString(
            periodo,
            fontSubtitulo,
            XBrushes.Black,
            new XRect(
                MargenIzquierdo,
                MargenSuperior +
                54,
                ancho / 2,
                14
            ),
            XStringFormats
                .TopLeft
        );


        // =================================================
        // GENERACIÓN
        // =================================================

        gfx.DrawString(
            $"Generado: " +
            $"{DateTime.Now:dd/MM/yyyy HH:mm}",
            fontSubtitulo,
            new XSolidBrush(
                GrisTexto
            ),
            new XRect(
                MargenIzquierdo +
                ancho / 2,
                MargenSuperior +
                54,
                ancho / 2,
                14
            ),
            XStringFormats
                .TopRight
        );


        return
            MargenSuperior +
            80;
    }


    // =====================================================
    // CABECERA TABLA
    // =====================================================

    private static double DibujarCabeceraTabla(
        XGraphics gfx,
        double y,
        IReadOnlyList<ColumnaPdf> columnas,
        XFont font)
    {
        const double alto =
            26;


        var x =
            MargenIzquierdo;


        foreach (
            var columna
            in columnas
        )
        {
            gfx.DrawRectangle(
                new XSolidBrush(
                    AzulPrincipal
                ),
                x,
                y,
                columna.Ancho,
                alto
            );


            gfx.DrawRectangle(
                new XPen(
                    XColors.White,
                    0.3
                ),
                x,
                y,
                columna.Ancho,
                alto
            );


            DibujarTextoCelda(
                gfx,
                columna.Titulo,
                font,
                XBrushes.White,
                x +
                3,
                y +
                3,
                columna.Ancho -
                6,
                alto -
                6
            );


            x +=
                columna.Ancho;
        }


        return
            y +
            alto;
    }


    // =====================================================
    // FILA
    // =====================================================

    private static void DibujarFila(
        XGraphics gfx,
        double y,
        double alto,
        IReadOnlyList<ColumnaPdf> columnas,
        IReadOnlyList<string> valores,
        XFont font)
    {
        var x =
            MargenIzquierdo;


        for (
            var i = 0;
            i <
            columnas.Count;
            i++
        )
        {
            var columna =
                columnas[i];


            var valor =
                i <
                valores.Count
                    ? valores[i]
                    : string.Empty;


            gfx.DrawRectangle(
                XBrushes.White,
                x,
                y,
                columna.Ancho,
                alto
            );


            gfx.DrawRectangle(
                new XPen(
                    GrisBorde,
                    0.4
                ),
                x,
                y,
                columna.Ancho,
                alto
            );


            var brush =
                ObtenerBrushTexto(
                    columna.Titulo,
                    valor
                );


            DibujarTextoCelda(
                gfx,
                valor,
                font,
                brush,
                x +
                3,
                y +
                3,
                columna.Ancho -
                6,
                alto -
                6
            );


            x +=
                columna.Ancho;
        }
    }


    // =====================================================
    // TEXTO DENTRO DE CELDA
    //
    // PDFsharp puro no realiza wrapping automático
    // equivalente al HTML, así que aquí recortamos
    // cuidadosamente según ancho disponible.
    // =====================================================

    private static void DibujarTextoCelda(
        XGraphics gfx,
        string texto,
        XFont font,
        XBrush brush,
        double x,
        double y,
        double ancho,
        double alto)
    {
        var valor =
            texto ??
            string.Empty;


        var textoAjustado =
            RecortarTexto(
                gfx,
                valor,
                font,
                ancho
            );


        gfx.DrawString(
            textoAjustado,
            font,
            brush,
            new XRect(
                x,
                y,
                ancho,
                alto
            ),
            XStringFormats
                .CenterLeft
        );
    }


    // =====================================================
    // RECORTAR TEXTO
    // =====================================================

    private static string RecortarTexto(
        XGraphics gfx,
        string texto,
        XFont font,
        double anchoDisponible)
    {
        if (
            string.IsNullOrWhiteSpace(
                texto
            )
        )
        {
            return string.Empty;
        }


        if (
            gfx.MeasureString(
                texto,
                font
            )
            .Width <=
            anchoDisponible
        )
        {
            return texto;
        }


        const string sufijo =
            "...";


        var valor =
            texto;


        while (
            valor.Length >
            1
        )
        {
            valor =
                valor[..^1];


            var candidato =
                valor +
                sufijo;


            if (
                gfx.MeasureString(
                    candidato,
                    font
                )
                .Width <=
                anchoDisponible
            )
            {
                return candidato;
            }
        }


        return sufijo;
    }


    // =====================================================
    // PIE
    // =====================================================

    private static void DibujarPiePagina(
        XGraphics gfx,
        PdfPage pagina,
        int numeroPagina,
        XFont font)
    {
        var y =
            pagina.Height.Point -
            MargenInferior;


        gfx.DrawLine(
            new XPen(
                GrisBorde,
                0.5
            ),
            MargenIzquierdo,
            y -
            5,
            pagina.Width.Point -
            MargenDerecho,
            y -
            5
        );


        gfx.DrawString(
            "Sistema de Control de Asistencia",
            font,
            new XSolidBrush(
                GrisTexto
            ),
            new XRect(
                MargenIzquierdo,
                y,
                250,
                12
            ),
            XStringFormats
                .TopLeft
        );


        gfx.DrawString(
            $"Página {numeroPagina}",
            font,
            new XSolidBrush(
                GrisTexto
            ),
            new XRect(
                pagina.Width.Point -
                MargenDerecho -
                100,
                y,
                100,
                12
            ),
            XStringFormats
                .TopRight
        );
    }


    // =====================================================
    // COLOR TEXTO SEGÚN ESTADO
    // =====================================================

    private static XBrush ObtenerBrushTexto(
        string tituloColumna,
        string valor)
    {
        if (
            tituloColumna.Equals(
                "Estado entrada",
                StringComparison.OrdinalIgnoreCase
            ) &&
            valor.StartsWith(
                "Atraso",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return new XSolidBrush(
                Rojo
            );
        }


        if (
            tituloColumna.Equals(
                "Jornada",
                StringComparison.OrdinalIgnoreCase
            ) &&
            valor.Equals(
                "Completa",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            return new XSolidBrush(
                Verde
            );
        }


        return new XSolidBrush(
            GrisTexto
        );
    }


    // =====================================================
    // HORA
    // =====================================================

    private static string FormatearHora(
        string? hora)
    {
        if (
            string.IsNullOrWhiteSpace(
                hora
            )
        )
        {
            return "-";
        }


        return hora.Length >=
            5
                ? hora[..5]
                : hora;
    }


    // =====================================================
    // NOMBRE TIPO
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
    // COLUMNA
    // =====================================================

    private sealed class ColumnaPdf
    {
        public string Titulo { get; }

        public double Ancho { get; }


        public ColumnaPdf(
            string titulo,
            double ancho)
        {
            Titulo =
                titulo;

            Ancho =
                ancho;
        }
    }
}