using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using ParagonAtmLibrary.Models;

namespace ParagonAtmLibrary.Services;

public class AtmService : IAtmService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AgentService> _logger;
    private readonly HttpClient _httpClient;

    public AtmService(IConfiguration configuration,
                      ILogger<AgentService> logger,
                      HttpClient httpClient)
    {
        _config = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public record AtmServices(List<AtmServiceModel> services);
    public record AuditData(string auditData);
    public record SwitchState(string status);

    public async Task<List<AtmServiceModel>> GetServicesAsync()
    {
        try
        {
            _logger.LogInformation($"Get ATM services list...");

            using HttpResponseMessage response =
                await _httpClient.PostAsync($"{_config["ApiEndpoint:Atm"]}/get-services", null);

            if (response.IsSuccessStatusCode)
            {
                AtmServices result = await response.Content.ReadFromJsonAsync<AtmServices>();
                _logger.LogInformation($"Success [{response.StatusCode}] -- /get-services");
                return result.services;
            }
            else
            {
                _logger.LogError($"Failed [{response.StatusCode}] -- /get-services");
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to get ATM services list -- [{e.Message}]");
            return null;
        }
    }

    public async Task<string> GetDeviceStateAsync(DeviceModel device)
    {
        try
        {
            _logger.LogInformation($"Get device state [{device.DeviceName}]...");

            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:Atm"]}/get-device-state", device);

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Success [{response.StatusCode}] -- /get-device-state");
                return result;
            }
            else
            {
                _logger.LogError($"Failed [{response.StatusCode}] -- /get-device-state");
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to get device state -- [{e.Message}]");
            return null;
        }
    }

    public async Task<bool> InsertCardAsync(CardModel insertCard)
    {
        _logger.LogInformation($"Insert card [{insertCard.CardId}] into reader [{insertCard.CardReaderName}]...");
        return await AtmRequestAsync("insert-card", insertCard);
    }

    public async Task<AuditData> TakeCardAsync()
    {
        try
        {
            _logger.LogInformation($"Take card...");

            using HttpResponseMessage response =
                await _httpClient.PostAsync($"{_config["ApiEndpoint:Atm"]}/take-card", null);

            if (response.IsSuccessStatusCode)
            {
                AuditData result = await response.Content.ReadFromJsonAsync<AuditData>();
                _logger.LogInformation($"Success [{response.StatusCode}] -- /take-card");
                return result;
            }
            else
            {
                _logger.LogError($"Failed [{response.StatusCode}] -- /take-card");
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to take card -- [{e.Message}]");
            return null;
        }
    }

    public async Task<bool> PressKeyAsync(PressKeyModel keyPress)
    {
        _logger.LogInformation($"Press key [{keyPress.PinpadKeys}] on pinpad [{keyPress.PinpadName}]...");
        return await AtmRequestAsync("press-key", keyPress);
    }

    public async Task<bool> PressTtuKeyAsync(PressTtuKeyModel keyPress)
    {
        _logger.LogInformation($"Press key [{keyPress.TextTerminalUnitKey}] on TTU [{keyPress.TextTerminalUnitName}]...");
        return await AtmRequestAsync("press-ttu-key", keyPress);
    }

    public async Task<PinpadKeysModel> GetPinpadKeysAsync(string pinpadName)
    {
        try
        {
            _logger.LogInformation($"Get available keys for [{pinpadName}]...");

            var data = new
            {
                pinpadName = pinpadName
            };

            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:Atm"]}/get-pin-pad-keys", data);

            if (response.IsSuccessStatusCode)
            {
                PinpadKeysModel result = await response.Content.ReadFromJsonAsync<PinpadKeysModel>();
                _logger.LogInformation($"Success [{response.StatusCode}] -- /get-pin-pad-keys");
                return result;
            }
            else
            {
                _logger.LogError($"Failed [{response.StatusCode}] -- /get-pin-pad-keys");
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to get pinpad keys -- [{e.Message}]");
            return null;
        }
    }

    public async Task<bool> ChangeOperatorSwitchAsync(bool supervisor)
    {
        if (supervisor)
        {
            _logger.LogInformation($"Change operator switch to SUPERVISOR mode...");

            var data = new
            {
                mode = "Supervisor"
            };

            return await AtmRequestAsync("change-operator-switch", data);
        }
        else
        {
            _logger.LogInformation($"Change operator switch to RUN mode...");

            var data = new
            {
                mode = "Run"
            };

            return await AtmRequestAsync("change-operator-switch", data);
        }
    }

    public async Task<bool> PushOperatorSwitchAsync()
    {
        _logger.LogInformation($"Push operator switch button...");
        return await AtmRequestAsync("push-operator-switch", "");
    }

    public async Task<bool> EnterDieboldSupervisorModeAsync()
    {
        _logger.LogInformation($"Enter Diebold supervisor mode...");
        return await AtmRequestAsync("enter-dn-supervisor-mode", "");
    }

    public async Task<bool> ExitDieboldSupervisorModeAsync()
    {
        _logger.LogInformation($"Exit Diebold supervisor mode...");
        return await AtmRequestAsync("exit-dn-supervisor-mode", "");
    }

    public async Task<SwitchState> OperatorSwitchStatusAsync(string deviceName)
    {
        try
        {
            _logger.LogInformation($"Get operator switch status for [{deviceName}]...");

            var data = new
            {
                deviceName = deviceName
            };

            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:Atm"]}/operator-switch-status", data);

            if (response.IsSuccessStatusCode)
            {
                SwitchState result = await response.Content.ReadFromJsonAsync<SwitchState>();
                _logger.LogInformation($"Success [{response.StatusCode}] -- /operator-switch-status");
                return result;
            }
            else
            {
                _logger.LogError($"Failed [{response.StatusCode}] -- /operator-switch-status");
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to get operator switch status -- [{e.Message}]");
            return null;
        }
    }

    public async Task<bool> InsertMediaAsync(MediaModel insertMedia)
    {
        _logger.LogInformation($"Insert media [{insertMedia.MediaId}] into device [{insertMedia.DeviceName}]...");
        return await AtmRequestAsync("insert-media", insertMedia);
    }

    public async Task<AuditData> TakeMediaAsync(string deviceName, int count)
    {
        try
        {
            _logger.LogInformation($"Take media...");

            var takeMedia = new
            {
                deviceName = deviceName,
                count = count
            };

            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:Atm"]}/take-media", takeMedia);

            if (response.IsSuccessStatusCode)
            {
                AuditData result = await response.Content.ReadFromJsonAsync<AuditData>();
                _logger.LogInformation($"Success [{response.StatusCode}] -- /take-media");
                return result;
            }
            else
            {
                _logger.LogError($"Failed [{response.StatusCode}] -- /take-media");
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to take media -- [{e.Message}]");
            return null;
        }
    }

    public async Task<ReceiptModel> TakeReceiptAsync(string deviceName)
    {
        try
        {
            _logger.LogInformation($"Take receipt...");

            var takeReceipt = new
            {
                deviceName = deviceName,
                runOcr = true
            };

            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:Atm"]}/take-receipt", takeReceipt);

            if (response.IsSuccessStatusCode)
            {
                ReceiptModel result = await response.Content.ReadFromJsonAsync<ReceiptModel>();
                _logger.LogInformation($"Success [{response.StatusCode}] -- /take-receipt");
                return result;
            }
            else
            {
                _logger.LogInformation($"Failed [{response.StatusCode}] -- /take-receipt");
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to take receipt -- [{e.Message}]");
            return null;
        }
    }

    public async Task<bool> RecoverAsync()
    {
        _logger.LogInformation($"Recover ATM from bad automation state...");
        return await AtmRequestAsync("recover", "");
    }

    private async Task<bool> AtmRequestAsync<T>(string endpoint, T content)
    {
        try
        {
            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:Atm"]}/{endpoint}", content);

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
