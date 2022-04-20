namespace ConsoleUI.Interfaces;

public interface IEdgeConsumerTransactionService
{
    Task BalanceInquiry(string cardId,
                        string cardPin,
                        string language,
                        string accountType,
                        string accountName,
                        string receiptOption);
}