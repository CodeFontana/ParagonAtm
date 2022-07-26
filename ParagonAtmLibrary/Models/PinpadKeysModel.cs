namespace ParagonAtmLibrary.Models;

public class PinpadKeysModel
{
    [JsonPropertyName("supportedKeys")]
    public string SupportedKeys { get; set; }

    [JsonPropertyName("enabledKeys")]
    public string EnabledKeys { get; set; }
}
