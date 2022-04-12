using System.Text.Json.Serialization;

namespace ParagonAtmLibrary.Models;

public class MediaModel
{
    [JsonPropertyName("mediaId")]
    public string MediaId { get; set; }

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; }

    [JsonPropertyName("deviceType")]
    public string DeviceType { get; set; }
}
