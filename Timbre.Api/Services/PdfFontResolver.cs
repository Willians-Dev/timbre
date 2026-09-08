using PdfSharp.Fonts;


namespace Timbre.Api.Services;


public class PdfFontResolver
    : IFontResolver
{
    // =====================================================
    // NOMBRES INTERNOS
    //
    // Son identificadores utilizados por PDFsharp.
    // No corresponden necesariamente al nombre físico
    // de la familia tipográfica.
    // =====================================================

    private const string Regular =
        "LiberationSans-Regular";


    private const string Bold =
        "LiberationSans-Bold";


    private readonly string
        _fontsPath;


    // =====================================================
    // CONSTRUCTOR
    // =====================================================

    public PdfFontResolver(
        IWebHostEnvironment environment
    )
    {
        _fontsPath =
            Path.Combine(
                environment
                    .ContentRootPath,

                "Fonts"
            );
    }


    // =====================================================
    // RESOLVER TIPO DE FUENTE
    //
    // ReportePdfService actualmente puede solicitar Arial.
    //
    // En Linux/Azure redirigimos esa solicitud hacia
    // Liberation Sans.
    // =====================================================

    public FontResolverInfo ResolveTypeface(
        string familyName,
        bool isBold,
        bool isItalic
    )
    {
        if (isBold)
        {
            return new FontResolverInfo(
                Bold
            );
        }


        return new FontResolverInfo(
            Regular
        );
    }


    // =====================================================
    // OBTENER ARCHIVO DE FUENTE
    // =====================================================

    public byte[] GetFont(
        string faceName
    )
    {
        var archivo =
            faceName switch
            {
                Regular =>
                    "LiberationSans-Regular.ttf",

                Bold =>
                    "LiberationSans-Bold.ttf",

                _ =>
                    throw new
                        InvalidOperationException(
                            $"Fuente PDF no reconocida: {faceName}"
                        )
            };


        var ruta =
            Path.Combine(
                _fontsPath,
                archivo
            );


        if (
            !File.Exists(
                ruta
            )
        )
        {
            throw new
                FileNotFoundException(
                    "No se encontró la fuente requerida " +
                    $"para generar el PDF: {archivo}"
                );
        }


        return File.ReadAllBytes(
            ruta
        );
    }
}