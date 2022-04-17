using System.Text.Json;

namespace ConsoleUI.Models;

public class TransactionModel
{
    public string Name { get; set; }
    public TransactionOptionsModel Options { get; set; }
    public List<TransactionScreenFlowModel> ScreenFlow { get; set; }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}
