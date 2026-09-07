namespace Timbre.Api.Options;

public class FaceAiOptions
{
    public const string SectionName = "FaceAI";

    public bool Habilitado { get; set; } = true;

    public string BaseUrl { get; set; } =
        "http://127.0.0.1:8000";

    public int TimeoutSegundos { get; set; } = 30;
}