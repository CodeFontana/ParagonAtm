namespace ConsoleUI.Models;

public class PlaylistOptionsModel
{
    public int Repeat { get; set; } = 1;
    public int RepeatDelay { get; set; } = 10000;
    public bool Shuffle { get; set; } = false;
}
