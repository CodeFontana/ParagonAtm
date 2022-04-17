using System.Text.Json;

namespace ConsoleUI.Models;

public class PlaylistModel
{
    public string Name { get; set; }
    public PlaylistOptionsModel Options { get; set; }
    public List<string> Transactions { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}
