namespace ParagonAtmLibrary.Interfaces;

public interface IClientService
{
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task<bool> DispatchToIdleAsync(string saveFolder = null);
    Task<bool> SaveScreenshotAsync(string folder, string screenName = "");
    Task<bool> StartAtmFromDesktopAsync();
    Task TakeAllMediaAsync(string saveFolder = null);
}