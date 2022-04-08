using System.Text.Json.Serialization;

namespace ParagonAtmLibrary.Models;

public class ScreenJpegModel
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; }
}
