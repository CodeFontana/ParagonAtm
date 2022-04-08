using System.Text.Json.Serialization;

namespace ParagonAtmLibrary.Models;

public class DeviceModel
{
    public DeviceModel(string deviceName)
    {
        DeviceName = deviceName;
    }

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; }
}
