using System.Text.Json.Serialization;

namespace Timbre.Api.DTOs.FaceAI;

public class FaceEmbeddingResponseDto
{
    [JsonPropertyName("rostro_detectado")]
    public bool RostroDetectado { get; set; }

    [JsonPropertyName("cantidad_rostros")]
    public int CantidadRostros { get; set; }

    [JsonPropertyName("dimension_embedding")]
    public int DimensionEmbedding { get; set; }

    [JsonPropertyName("embedding")]
    public List<float> Embedding { get; set; } = [];

    [JsonPropertyName("confianza")]
    public float Confianza { get; set; }

    [JsonPropertyName("mensaje")]
    public string Mensaje { get; set; } = string.Empty;
}