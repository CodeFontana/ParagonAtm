using ParagonAtmLibrary.Models;

namespace ConsoleUI.Interfaces;
public interface IClientService
{
    TerminalModel VirtualAtm { get; set; }
    WebFastUserModel WebFastUser { get; set; }

    Task<bool> ConnectAsync();
    Task DispatchToIdle();
    Task<bool> SaveScreenShot(string folder);
}