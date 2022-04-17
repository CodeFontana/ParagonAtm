namespace ConsoleUI.Models;

public class TransactionScreenFlowModel
{
    public string Screen { get; set; }
    public int TimeoutSeconds { get; set; }
    public int RefreshSeconds { get; set; }
    public string ActionType { get; set; }
    public string ActionValue { get; set; }
}