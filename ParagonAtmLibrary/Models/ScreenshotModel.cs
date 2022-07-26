namespace ParagonAtmLibrary.Models;

public class ScreenshotModel
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; }
}
