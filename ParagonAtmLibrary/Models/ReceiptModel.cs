namespace ParagonAtmLibrary.Models;

public class ReceiptModel
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; }

    [JsonPropertyName("result")]
    public string Result { get; set; }

    [JsonPropertyName("ocrData")]
    public OcrDataModel OcrData { get; set; }
}
