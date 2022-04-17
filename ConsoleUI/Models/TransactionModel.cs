namespace ConsoleUI.Models;

public class TransactionModel
{
    public string Name { get; set; }
    public TransactionOptionsModel Options { get; set; }
    public List<TransactionScreenFlowModel> ScreenFlow { get; set; }
}
