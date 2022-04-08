using System.Text.Json.Serialization;

namespace ParagonAtmLibrary.Models;

public class InsertCardModel
{
    public InsertCardModel(string cardId, string cardReaderName)
    {
        CardId = cardId;
        CardReaderName = cardReaderName;
    }

    [JsonPropertyName("cardId")]
    public string CardId { get; set; }

    [JsonPropertyName("cardReaderName")]
    public string CardReaderName { get; set; }
}
