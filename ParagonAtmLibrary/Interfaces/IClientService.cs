namespace ParagonAtmLibrary.Interfaces;

public interface IClientService
{
    Task<bool> ConnectAsync();
    Task DisconnectAsync();
    Task<bool> DispatchToIdle();
    Task<bool> SaveScreenShot(string folder);
    Task TakeAllMedia();
}