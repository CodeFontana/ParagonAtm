namespace ConsoleUI.Interfaces;

public interface IVistaBalanceInquiryService
{
    Task BalanceInquiry(CancellationToken cancelToken,
                        string cardId = "f2305283-bb84-49fe-aba6-cd3f7bcfa5ba",
                        string cardPin = "1234",
                        string receiptOption = "Display Balance",
                        string accountType = "Checking",
                        string accountName = "Checking|T");
}