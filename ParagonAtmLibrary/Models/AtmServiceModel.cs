using System.Text.Json.Serialization;

namespace ParagonAtmLibrary.Models;

public class AtmServiceModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("deviceType")]
    public string DeviceType { get; set; }

    [JsonPropertyName("isOpen")]
    public bool IsOpen { get; set; }

    [JsonPropertyName("media")]
    public int Media { get; set; }
}
