namespace ConsoleUI.Interfaces;

public interface IEdgeConsumerTransactionService
{
    Task BalanceInquiry(CancellationToken cancelToken,
                        string cardId,
                        string cardPin,
                        string language,
                        string accountType,
                        string accountName,
                        string receiptOption);
}