namespace ConsoleUI.Interfaces;

public interface IActivateEnterpriseBalanceInquiryService
{
    Task BalanceInquiry(CancellationToken cancelToken,
                        string cardId,
                        string cardPin,
                        string language,
                        string accountType,
                        string accountName);
}
