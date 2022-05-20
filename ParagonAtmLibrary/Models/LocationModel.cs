using System.Text.Json.Serialization;

namespace ParagonAtmLibrary.Models;

public class LocationModel
{
    [JsonPropertyName("found")]
    public bool Found { get; set; }

    [JsonPropertyName("point")]
    public ScreenCoordinates Location { get; set; }
}

public class ScreenCoordinates
{
    public ScreenCoordinates()
    {

    }
    
    public ScreenCoordinates(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public float x { get; set; }
    public float y { get; set; }
}



