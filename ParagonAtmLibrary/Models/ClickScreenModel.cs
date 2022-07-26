namespace ParagonAtmLibrary.Models;

public class ClickScreenModel
{
    public ClickScreenModel(float x, float y)
    {
        XCoordinate = x;
        YCoordinate = y;
    }

    public ClickScreenModel(LocationModel s)
    {
        XCoordinate = s.Location.x;
        YCoordinate = s.Location.y;
    }

    public ClickScreenModel(ScreenCoordinates s)
    {
        XCoordinate = s.x;
        YCoordinate = s.y;
    }

    [JsonPropertyName("x")]
    public float XCoordinate { get; set; }

    [JsonPropertyName("y")]
    public float YCoordinate { get; set; }
}
