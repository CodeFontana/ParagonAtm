namespace ConsoleUI.Interfaces;

public interface IConsumerTransactionService
{
    Task BalanceInquiry(string cardId,
                        string cardPin,
                        string language,
                        string accountType,
                        string accountName,
                        string receiptOption);
}