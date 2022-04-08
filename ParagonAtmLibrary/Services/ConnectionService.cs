using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace ParagonAtmLibrary.Services;

public class ConnectionService
{
    private readonly IConfiguration _config;
    private readonly ILogger<ConnectionService> _logger;
    private readonly HttpClient _httpClient;

    public ConnectionService(IConfiguration configuration,
                             ILogger<ConnectionService> logger,
                             HttpClient httpClient)
    {
        _config = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> OpenAsync()
    {
        _logger.LogInformation($"Open connection...");
        return await ConnectionRequestAsync("open", "");
    }

    public async Task<bool> CloseAsync()
    {
        _logger.LogInformation($"Close connection...");
        return await ConnectionRequestAsync("close", "");
    }

    public async Task<bool> SaveCloseAsync()
    {
        _logger.LogInformation($"Save Virtual ATM profile and close connection...");
        return await ConnectionRequestAsync("save-close", "");
    }

    public async Task<bool> SaveCloseRebootAsync()
    {
        _logger.LogInformation($"Save Virtual ATM profile, close connection and reboot...");
        return await ConnectionRequestAsync("save-close-reboot", "");
    }

    public async Task<bool> CloseRebootAsync()
    {
        _logger.LogInformation($"Close connection and reboot...");
        return await ConnectionRequestAsync("close-reboot", "");
    }

    private async Task<bool> ConnectionRequestAsync<T>(string endpoint, T content)
    {
        try
        {
            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:Connection"]}/{endpoint}", content);

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
