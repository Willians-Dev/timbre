using Microsoft.EntityFrameworkCore;
using Timbre.Api.Data;
using Timbre.Api.DTOs.FaceAI;

namespace Timbre.Api.Services;

public class IdentificacionFacialService
{
    private readonly TimbreDbContext _context;

    // Umbral provisional.
    // Luego lo moveremos a configuración/calibración.
    private const float UmbralSimilitud = 0.45f;

    public IdentificacionFacialService(
        TimbreDbContext context)
    {
        _context = context;
    }

    public async Task<IdentificacionFacialResultadoDto>
        IdentificarAsync(
            List<float> embeddingCapturado,
            float confianzaDeteccion,
            CancellationToken cancellationToken = default)
    {
        if (embeddingCapturado is null ||
            embeddingCapturado.Count == 0)
        {
            throw new ArgumentException(
                "El embedding capturado no es válido."
            );
        }

        var rostros =
            await _context.RostroEmpleado
                .AsNoTracking()
                .Where(r =>
                    r.Activo &&
                    r.Embedding != null &&
                    r.IdEmpleadoNavigation.Activo)
                .Select(r => new
                {
                    r.IdRostro,
                    r.IdEmpleado,
                    r.Embedding,
                    r.Modelo,
                    r.DimensionEmbedding,

                    Nombres =
                        r.IdEmpleadoNavigation.Nombres,

                    Apellidos =
                        r.IdEmpleadoNavigation.Apellidos
                })
                .ToListAsync(
                    cancellationToken
                );

        if (rostros.Count == 0)
        {
            return new IdentificacionFacialResultadoDto
            {
                Reconocido = false,
                Similitud = 0,
                Umbral = UmbralSimilitud,
                ConfianzaDeteccion =
                    confianzaDeteccion,
                Mensaje =
                    "No existen empleados con rostro activo registrado."
            };
        }

        float mejorSimilitud =
            float.MinValue;

        long? mejorIdEmpleado = null;
        string? mejorEmpleado = null;
        string? mejorModelo = null;

        foreach (var rostro in rostros)
        {
            if (rostro.Embedding is null ||
                rostro.Embedding.Count == 0)
            {
                continue;
            }

            if (rostro.DimensionEmbedding.HasValue &&
                rostro.DimensionEmbedding.Value !=
                    embeddingCapturado.Count)
            {
                continue;
            }

            var similitud =
                CalcularSimilitudCoseno(
                    embeddingCapturado,
                    rostro.Embedding
                );

            if (similitud >
                mejorSimilitud)
            {
                mejorSimilitud =
                    similitud;

                mejorIdEmpleado =
                    rostro.IdEmpleado;

                mejorEmpleado =
                    $"{rostro.Nombres} {rostro.Apellidos}";

                mejorModelo =
                    rostro.Modelo;
            }
        }

        if (!mejorIdEmpleado.HasValue)
        {
            return new IdentificacionFacialResultadoDto
            {
                Reconocido = false,
                Similitud = 0,
                Umbral = UmbralSimilitud,
                ConfianzaDeteccion =
                    confianzaDeteccion,
                Mensaje =
                    "No existen embeddings compatibles para comparar."
            };
        }

        var reconocido =
            mejorSimilitud >=
            UmbralSimilitud;

        return new IdentificacionFacialResultadoDto
        {
            Reconocido =
                reconocido,

            IdEmpleado =
                reconocido
                    ? mejorIdEmpleado
                    : null,

            Empleado =
                reconocido
                    ? mejorEmpleado
                    : null,

            Similitud =
                MathF.Round(
                    mejorSimilitud,
                    4
                ),

            Umbral =
                UmbralSimilitud,

            ConfianzaDeteccion =
                MathF.Round(
                    confianzaDeteccion,
                    4
                ),

            Modelo =
                reconocido
                    ? mejorModelo
                    : null,

            Mensaje =
                reconocido
                    ? "Empleado identificado correctamente."
                    : "Rostro no reconocido."
        };
    }

    private static float
        CalcularSimilitudCoseno(
            IReadOnlyList<float> vectorA,
            IReadOnlyList<float> vectorB)
    {
        if (vectorA.Count !=
            vectorB.Count)
        {
            throw new ArgumentException(
                "Los embeddings tienen dimensiones diferentes."
            );
        }

        double producto = 0;
        double normaA = 0;
        double normaB = 0;

        for (var i = 0;
             i < vectorA.Count;
             i++)
        {
            producto +=
                vectorA[i] *
                vectorB[i];

            normaA +=
                vectorA[i] *
                vectorA[i];

            normaB +=
                vectorB[i] *
                vectorB[i];
        }

        if (normaA == 0 ||
            normaB == 0)
        {
            return 0;
        }

        return (float)(
            producto /
            (
                Math.Sqrt(normaA) *
                Math.Sqrt(normaB)
            )
        );
    }
}