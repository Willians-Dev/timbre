using System.Text.Json.Serialization;

namespace Timbre.Api.DTOs.FaceAI;

public class FaceEnrollmentResponseDto
{
    [JsonPropertyName("enrolamiento_valido")]
    public bool EnrolamientoValido { get; set; }

    [JsonPropertyName("cantidad_muestras")]
    public int CantidadMuestras { get; set; }

    [JsonPropertyName("dimension_embedding")]
    public int DimensionEmbedding { get; set; }

    [JsonPropertyName("embedding")]
    public List<float> Embedding { get; set; } = [];

    [JsonPropertyName("mensaje")]
    public string Mensaje { get; set; } =
        string.Empty;
}