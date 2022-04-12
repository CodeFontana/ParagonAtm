using System.Text.Json.Serialization;

namespace ParagonAtmLibrary.Models;

public class PressKeyModel
{
    public PressKeyModel(string pinpadName, string pinpadKeys)
    {
        PinpadName = pinpadName;
        PinpadKeys = pinpadKeys;
    }
    
    [JsonPropertyName("pinpadName")]
    public string PinpadName { get; set; }

    [JsonPropertyName("pinpadKeys")]
    public string PinpadKeys { get; set; }
}
