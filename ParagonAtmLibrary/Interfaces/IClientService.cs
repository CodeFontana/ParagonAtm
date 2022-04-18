namespace ParagonAtmLibrary.Interfaces;

public interface IClientService
{
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task<bool> DispatchToIdleAsync();
    Task<bool> SaveScreenshotAsync(string folder);
    Task TakeAllMediaAsync();
}