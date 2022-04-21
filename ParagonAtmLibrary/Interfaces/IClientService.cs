namespace ParagonAtmLibrary.Interfaces;

public interface IClientService
{
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task<bool> DispatchToIdleAsync(string saveFolder = null);
    Task<bool> SaveScreenshotAsync(string folder);
    Task TakeAllMediaAsync(string saveFolder = null);
}