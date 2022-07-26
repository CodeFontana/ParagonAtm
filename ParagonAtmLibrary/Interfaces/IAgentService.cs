namespace ParagonAtmLibrary.Interfaces;
public interface IAgentService
{
    Task<bool> CloseSesisonAsync();
    Task<AgentStatusModel> GetAgentStatusAsync();
    Task<bool> GetUserGroupsAsync(WebFastUserModel webFastUser);
    Task<bool> OpenHwProfileAsync(string hwProfileId);
    Task<bool> OpenSesisonAsync(WebFastUserModel webFastUser);
    Task<bool> StartAtmAppAsync(string startupApp);
}