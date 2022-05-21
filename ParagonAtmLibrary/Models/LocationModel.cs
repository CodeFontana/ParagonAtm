using System.Text.Json.Serialization;

namespace ParagonAtmLibrary.Models;

public class LocationModel
{
    [JsonPropertyName("found")]
    public bool Found { get; set; }

    [JsonPropertyName("point")]
    public ScreenCoordinates Location { get; set; }
}
