using ParagonAtmLibrary.Models;

namespace ParagonAtmLibrary.Interfaces;
public interface IAgentService
{
    Task<bool> CloseSesisonAsync();
    Task<AgentStatusModel> GetAgentStatusAsync();
    Task<bool> GetUserGroupsAsync(WebFastUserModel webFastUser);
    Task<bool> OpenHwProfileAsync(TerminalModel virtualAtm);
    Task<bool> OpenSesisonAsync(WebFastUserModel webFastUser);
    Task<bool> StartAtmAppAsync(string startupApp);
}