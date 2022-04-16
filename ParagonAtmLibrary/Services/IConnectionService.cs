namespace ParagonAtmLibrary.Services;

public interface IConnectionService
{
    Task<bool> CloseAsync();
    Task<bool> CloseRebootAsync();
    Task<bool> OpenAsync();
    Task<bool> SaveCloseAsync();
    Task<bool> SaveCloseRebootAsync();
}