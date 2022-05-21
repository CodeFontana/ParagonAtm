namespace ParagonAtmLibrary.Models;

public class FindAndClickModel
{
    public FindAndClickModel(ScreenCoordinates screenCoordinates, string text, decimal confidence)
    {
        Location = screenCoordinates;
        Text = text;
        Confidence = confidence;
    }

    public ScreenCoordinates Location { get; set; }
    public string Text { get; set; }
    public decimal Confidence { get; set; }
}
