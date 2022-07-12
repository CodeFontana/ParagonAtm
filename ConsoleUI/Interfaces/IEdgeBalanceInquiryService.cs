namespace ConsoleUI.Interfaces;

public interface IEdgeBalanceInquiryService
{
    Task BalanceInquiry(CancellationToken cancelToken,
                        string cardId,
                        string cardPin,
                        string language,
                        string accountType,
                        string accountName,
                        string receiptOption);
}