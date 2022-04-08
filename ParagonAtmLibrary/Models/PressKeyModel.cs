using System.Text.Json.Serialization;

namespace ParagonAtmLibrary.Models;

public class PressKeyModel
{
    [JsonPropertyName("pinpadName")]
    public string PinpadName { get; set; }

    [JsonPropertyName("pinpadKeys")]
    public string PinpadKeys { get; set; }
}
