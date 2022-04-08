using System.Text.Json.Serialization;

namespace ParagonAtmLibrary.Models;

public class PressTtuKeyModel
{
    [JsonPropertyName("ttuName")]
    public string TextTerminalUnitName { get; set; }

    [JsonPropertyName("ttuKey")]
    public string TextTerminalUnitKey { get; set; }
}
