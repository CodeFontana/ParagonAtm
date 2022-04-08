using System.Text.Json.Serialization;

namespace ParagonAtmLibrary.Models;

public class ScreenTextLocationModel
{
    [JsonPropertyName("found")]
    public bool Found { get; set; }

    [JsonPropertyName("point")]
    public ScreenCoordinates Location { get; set; }
}

public class ScreenCoordinates
{
    public float x { get; set; }
    public float y { get; set; }
}



