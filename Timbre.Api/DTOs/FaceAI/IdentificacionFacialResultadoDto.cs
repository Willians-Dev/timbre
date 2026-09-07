namespace Timbre.Api.DTOs.FaceAI;

public class IdentificacionFacialResultadoDto
{
    public bool Reconocido { get; set; }

    public long? IdEmpleado { get; set; }

    public string? Empleado { get; set; }

    public float Similitud { get; set; }

    public float Umbral { get; set; }

    public float ConfianzaDeteccion { get; set; }

    public string? Modelo { get; set; }

    public string Mensaje { get; set; } = string.Empty;
}