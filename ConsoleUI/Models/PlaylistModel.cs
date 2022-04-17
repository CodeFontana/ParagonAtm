namespace ConsoleUI.Models;

public class PlaylistModel
{
    public string Name { get; set; }
    public PlaylistOptionsModel Options { get; set; }
    public List<string> Transactions { get; set; }
}
