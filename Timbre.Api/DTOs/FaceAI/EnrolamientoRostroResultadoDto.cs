namespace Timbre.Api.DTOs.FaceAI;

public class EnrolamientoRostroResultadoDto
{
    public long IdRostro { get; set; }

    public long IdEmpleado { get; set; }

    public string Empleado { get; set; } =
        string.Empty;

    public bool EnrolamientoValido { get; set; }

    public int CantidadMuestras { get; set; }

    public int DimensionEmbedding { get; set; }

    public string Modelo { get; set; } =
        string.Empty;

    public string Mensaje { get; set; } =
        string.Empty;
}