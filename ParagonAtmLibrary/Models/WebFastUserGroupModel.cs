namespace ParagonAtmLibrary.Models;

public class WebFastUserGroupModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }
}
