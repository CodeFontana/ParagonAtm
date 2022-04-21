namespace ParagonAtmLibrary.Interfaces;

public interface IClientService
{
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task<bool> DispatchToIdleAsync();
    bool SaveReceiptAsync(string folder, string receiptJpeg);
    Task<bool> SaveScreenshotAsync(string folder);
    Task TakeAllMediaAsync();
}