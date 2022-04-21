using System.Text.Json.Serialization;

namespace ParagonAtmLibrary.Models;

public class ReceiptModel
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("format")]
    public string format { get; set; }

    [JsonPropertyName("result")]
    public string result { get; set; }

    [JsonPropertyName("ocrData")]
    public OcrDataModel OcrData { get; set; }
}
