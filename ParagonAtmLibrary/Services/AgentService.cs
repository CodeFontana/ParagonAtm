using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ParagonAtmLibrary.Models;

namespace ParagonAtmLibrary.Services;

public class AgentService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AgentService> _logger;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public AgentService(IConfiguration configuration,
                        ILogger<AgentService> logger,
                        HttpClient httpClient)
    {
        _config = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(_config["Terminal:Host"]);
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    public record SessionToken(string sessionToken);
    public record WebFastUserGroups(List<WebFastUserGroupModel> groups);

    public async Task<AgentStatusModel> GetAgentStatusAsync()
    {
        try
        {
            _logger.LogInformation($"Get agent status...");

            using HttpResponseMessage response =
                await _httpClient.PostAsync($"{_config["ApiEndpoint:Agent"]}/get-agent-status", null);

            if (response.IsSuccessStatusCode)
            {
                AgentStatusModel result = await response.Content.ReadFromJsonAsync<AgentStatusModel>();
                _logger.LogInformation($"Success [{response.StatusCode}] -- /get-agent-status");
                return result;
            }
            else
            {
                _logger.LogInformation($"Failed [{response.StatusCode}] -- /get-agent-status");
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to get agent status -- [{e.Message}]");
            return null;
        }
    }

    public async Task<bool> GetUserGroupsAsync(WebFastUserModel webFastUser)
    {
        try
        {
            _logger.LogInformation($"Get user groups for {webFastUser.Username}...");

            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:Agent"]}/get-user-groups", webFastUser);

            if (response.IsSuccessStatusCode)
            {
                WebFastUserGroups result = await response.Content.ReadFromJsonAsync<WebFastUserGroups>();
                _logger.LogInformation($"Success [{response.StatusCode}] -- /get-user-groups");
                webFastUser.WebFastGroups = result.groups;
                return true;
            }
            else
            {
                _logger.LogInformation($"Failed [{response.StatusCode}] -- /get-user-groups");
                return false;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to get user groups -- [{e.Message}]");
            return false;
        }
    }

    public async Task<bool> OpenHwProfileAsync(TerminalModel virtualAtm)
    {
        _logger.LogInformation($"Open profile [HwProfile={virtualAtm.HwProfile}]...");

        var profileData = new
        {
            profileId = virtualAtm.HwProfile
        };

        return await AgentRequestAsync("open-profile", profileData);
    }

    public async Task<bool> StartAtmAppAsync(string startupApp)
    {
        _logger.LogInformation($"Start app [{startupApp}]...");

        var appData = new
        {
            runFileName = startupApp
        };

        return await AgentRequestAsync("start-atm", appData);
    }

    public async Task<bool> OpenSesisonAsync(WebFastUserModel webFastUser)
    {
        try
        {
            _logger.LogInformation($"Open session for {webFastUser.Username} [GroupId={webFastUser.GroupId}] on {_config["Terminal:Host"]}...");

            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:Agent"]}/open-session", webFastUser);

            if (response.IsSuccessStatusCode)
            {
                SessionToken result = await response.Content.ReadFromJsonAsync<SessionToken>();
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.sessionToken);
                webFastUser.SessionToken = result.sessionToken;
                return true;
            }
            else
            {
                _logger.LogError($"Failed to open session -- {response.ReasonPhrase}");
                return false;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to open session -- {e.Message}");
            return false;
        }
    }

    public async Task<bool> CloseSesisonAsync()
    {
        try
        {
            _logger.LogInformation("Close session...");

            using HttpResponseMessage response =
                await _httpClient.PostAsync($"{_config["ApiEndpoint:Agent"]}/close-session", null);

            if (response.IsSuccessStatusCode)
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _logger.LogInformation("Session closed");
                return true;
            }
            else
            {
                _logger.LogError($"Failed to close session -- [{response.ReasonPhrase}]");
                return false;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to close session -- [{e.Message}]");
            return false;
        }
    }

    private async Task<bool> AgentRequestAsync<T>(string endpoint, T content)
    {
        try
        {
            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:Agent"]}/{endpoint}", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Success [{response.StatusCode}] -- /{endpoint}");
                return true;
            }
            else
            {
                _logger.LogError($"Request failed [{response.StatusCode}] [/{endpoint}] -- {response.ReasonPhrase}");
                return false;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Request failed [/{endpoint}] -- {e.Message}");
            return false;
        }
    }
}
