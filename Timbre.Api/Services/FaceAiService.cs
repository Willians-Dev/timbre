using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Timbre.Api.DTOs.FaceAI;
using Timbre.Api.Options;

namespace Timbre.Api.Services;

public class FaceAiService
{
    private readonly HttpClient _httpClient;
    private readonly FaceAiOptions _options;

    public FaceAiService(
        HttpClient httpClient,
        IOptions<FaceAiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public bool EstaHabilitado =>
        _options.Habilitado;

    public async Task<FaceEnrollmentResponseDto>
        EnrolarAsync(
            IFormFile file1,
            IFormFile file2,
            IFormFile file3,
            CancellationToken cancellationToken = default)
    {
        if (!_options.Habilitado)
        {
            throw new InvalidOperationException(
                "El componente de reconocimiento facial está deshabilitado."
            );
        }

        using var contenido =
            new MultipartFormDataContent();

        await AgregarArchivoAsync(
            contenido,
            file1,
            "file1",
            cancellationToken
        );

        await AgregarArchivoAsync(
            contenido,
            file2,
            "file2",
            cancellationToken
        );

        await AgregarArchivoAsync(
            contenido,
            file3,
            "file3",
            cancellationToken
        );

        using var response =
            await _httpClient.PostAsync(
                "/face/enroll",
                contenido,
                cancellationToken
            );

        if (!response.IsSuccessStatusCode)
        {
            var detalle =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken
                    );

            throw new InvalidOperationException(
                $"FaceAI respondió con error " +
                $"{(int)response.StatusCode}: " +
                detalle
            );
        }

        var resultado =
            await response.Content
                .ReadFromJsonAsync
                <FaceEnrollmentResponseDto>(
                    cancellationToken:
                        cancellationToken
                );

        if (resultado is null)
        {
            throw new InvalidOperationException(
                "FaceAI devolvió una respuesta vacía."
            );
        }

        if (!resultado.EnrolamientoValido)
        {
            throw new InvalidOperationException(
                resultado.Mensaje
            );
        }

        if (resultado.Embedding is null ||
            resultado.Embedding.Count == 0)
        {
            throw new InvalidOperationException(
                "FaceAI no devolvió un embedding válido."
            );
        }

        if (resultado.DimensionEmbedding !=
            resultado.Embedding.Count)
        {
            throw new InvalidOperationException(
                "La dimensión del embedding devuelta por FaceAI es inconsistente."
            );
        }

        return resultado;
    }

    private static async Task AgregarArchivoAsync(
        MultipartFormDataContent contenido,
        IFormFile archivo,
        string nombreCampo,
        CancellationToken cancellationToken)
    {
        if (archivo is null ||
            archivo.Length == 0)
        {
            throw new ArgumentException(
                $"El archivo {nombreCampo} está vacío."
            );
        }

        var stream =
            archivo.OpenReadStream();

        var fileContent =
            new StreamContent(stream);

        if (!string.IsNullOrWhiteSpace(
            archivo.ContentType))
        {
            fileContent.Headers.ContentType =
                MediaTypeHeaderValue.Parse(
                    archivo.ContentType
                );
        }

        contenido.Add(
            fileContent,
            nombreCampo,
            archivo.FileName
        );

        await Task.CompletedTask;
    }

    public async Task<FaceEmbeddingResponseDto>
        GenerarEmbeddingAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
    {
        if (!_options.Habilitado)
        {
            throw new InvalidOperationException(
                "El componente de reconocimiento facial está deshabilitado."
            );
        }

        if (file is null || file.Length == 0)
        {
            throw new ArgumentException(
                "La imagen está vacía."
            );
        }

        using var contenido =
            new MultipartFormDataContent();

        await AgregarArchivoAsync(
            contenido,
            file,
            "file",
            cancellationToken
        );

        using var response =
            await _httpClient.PostAsync(
                "/face/embedding",
                contenido,
                cancellationToken
            );

        if (!response.IsSuccessStatusCode)
        {
            var detalle =
                await response.Content
                    .ReadAsStringAsync(
                        cancellationToken
                    );

            throw new InvalidOperationException(
                $"FaceAI respondió con error " +
                $"{(int)response.StatusCode}: " +
                detalle
            );
        }

        var resultado =
            await response.Content
                .ReadFromJsonAsync<FaceEmbeddingResponseDto>(
                    cancellationToken:
                        cancellationToken
                );

        if (resultado is null)
        {
            throw new InvalidOperationException(
                "FaceAI devolvió una respuesta vacía."
            );
        }

        if (!resultado.RostroDetectado)
        {
            throw new InvalidOperationException(
                resultado.Mensaje
            );
        }

        if (resultado.CantidadRostros != 1)
        {
            throw new InvalidOperationException(
                "La imagen debe contener exactamente un rostro."
            );
        }

        if (resultado.Embedding.Count == 0)
        {
            throw new InvalidOperationException(
                "FaceAI no devolvió un embedding facial."
            );
        }

        if (resultado.DimensionEmbedding !=
            resultado.Embedding.Count)
        {
            throw new InvalidOperationException(
                "La dimensión del embedding es inconsistente."
            );
        }

        return resultado;
    }
}