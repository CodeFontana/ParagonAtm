using System.Text.Json.Serialization;

namespace ParagonAtmLibrary.Models;

public class ScreenOcrDataModel
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("elements")]
    public Element[] Elements { get; set; }
}

public class Element
{
    public float x0 { get; set; }
    public float y0 { get; set; }
    public float x1 { get; set; }
    public float y1 { get; set; }
    public string text { get; set; }
    public Line[] lines { get; set; }
}

public class Line
{
    public float x0 { get; set; }
    public float y0 { get; set; }
    public float x1 { get; set; }
    public float y1 { get; set; }
    public string text { get; set; }
    public Word[] words { get; set; }
}

public class Word
{
    public float x0 { get; set; }
    public float y0 { get; set; }
    public float x1 { get; set; }
    public float y1 { get; set; }
    public string text { get; set; }
    public float confidence { get; set; }
}
