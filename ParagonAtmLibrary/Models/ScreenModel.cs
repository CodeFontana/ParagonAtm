namespace ParagonAtmLibrary.Models;

public class ScreenNameModel
{
    [JsonPropertyName("screenName")]
    public string ScreenName { get; set; }
}

public class ScreenModel
{
    [JsonPropertyName("screenName")]
    public string ScreenName { get; set; }

    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("bounds")]
    public Bounds Bounds { get; set; }
}

public class Bounds
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}
