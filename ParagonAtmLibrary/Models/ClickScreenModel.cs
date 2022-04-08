using System.Text.Json.Serialization;

namespace ParagonAtmLibrary.Models;

public class ClickScreenModel
{
    public ClickScreenModel(float x, float y)
    {
        XCoordinate = x;
        YCoordinate = y;
    }

    [JsonPropertyName("x")]
    public float XCoordinate { get; set; }

    [JsonPropertyName("y")]
    public float YCoordinate { get; set; }
}
