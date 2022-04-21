using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Interfaces;
using ParagonAtmLibrary.Models;
using System.Text.Json;
using static ParagonAtmLibrary.Services.AtmService;

namespace ParagonAtmLibrary.Services;

public class ClientService : IClientService
{
    private readonly IConfiguration _config;
    private readonly ILogger<ClientService> _logger;
    private readonly IAgentService _agentService;
    private readonly IConnectionService _connectionService;
    private readonly IVirtualMachineService _vmService;
    private readonly IAtmService _atmService;
    private readonly IAutomationService _autoService;
    private readonly List<AtmScreenModel> _atmScreens;
    private readonly WebFastUserModel _webFastUser;
    private readonly TerminalModel _virtualAtm;

    public ClientService(IConfiguration configuration,
                         ILogger<ClientService> logger,
                         IAgentService agentService,
                         IConnectionService connectionService,
                         IVirtualMachineService vmService,
                         IAtmService atmService,
                         IAutomationService autoService)
    {
        _config = configuration;
        _logger = logger;
        _agentService = agentService;
        _connectionService = connectionService;
        _vmService = vmService;
        _atmService = atmService;
        _autoService = autoService;
        _atmScreens = _config.GetSection("AvailableScreens").Get<List<AtmScreenModel>>();

        _virtualAtm = new()
        {
            Host = _config["Terminal:Host"],
            HwProfile = _config["Terminal:HwProfile"],
            StartupApps = _config.GetSection("Terminal:StartupApps").Get<List<string>>()
        };

        _webFastUser = new WebFastUserModel
        {
            Username = _config["WebFast:Username"],
            Password = _config["WebFast:Password"],
            GroupId = int.Parse(_config["WebFast:GroupId"])
        };
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            AgentStatusModel agentState = await _agentService.GetAgentStatusAsync();
            bool agentPaused = false;

            if (agentState == null)
            {
                _logger.LogError("Unable to query agent status, may not be running, unable to acquire session");
                return false;
            }
            else if (agentState.AgentStatus.ToUpper().Equals("IDLE") == false
                && agentState.AgentStatus.ToUpper().Equals("PAUSED") == false)
            {
                _logger.LogError("Agent must be in IDLE or PAUSED state to acquire session");
                
                if (await _atmService.RecoverAsync())
                {
                    _logger.LogInformation("Session recovered...");
                    return await ConnectAsync();
                }
                
                return false;
            }
            else if (agentState.AgentStatus.ToUpper().Equals("PAUSED"))
            {
                agentPaused = true;
            }

            if (await _agentService.OpenSesisonAsync(_webFastUser) == false)
            {
                _logger.LogError("Failed to acquire session");
                return false;
            }

            agentState = await _agentService.GetAgentStatusAsync();

            if (agentState.AgentStatus.ToUpper().Equals("APICONTROLLED") == false)
            {
                _logger.LogError($"Unexpected session state [{agentState.AgentStatus}], expecting [APICONTROLLED]");
                return false;
            }

            if (agentPaused == false)
            {
                await _agentService.OpenHwProfileAsync(_virtualAtm.HwProfile);
            }

            bool success = await _connectionService.OpenAsync();

            if (success == false)
            {
                _logger.LogError("Failed to open connection");
                return false;
            }

            _logger.LogInformation("Check if ATM app is running...");
            List<string> screenWords = await _autoService.GetScreenWordsAsync();

            if (screenWords == null)
            {
                _logger.LogError("ATM screen is not available");
                return false;
            }

            AtmScreenModel curScreen = _autoService.MatchScreen(_atmScreens, screenWords);

            if (curScreen == null)
            {
                foreach (string app in _virtualAtm.StartupApps)
                {
                    await _agentService.StartAtmAppAsync(app);
                }

                TimeSpan startupDelay = TimeSpan.FromSeconds(int.Parse(_config["Terminal:StartupDelaySeconds"]));
                _logger.LogInformation($"Delaying for {startupDelay.TotalSeconds} seconds while ATM app starts...");
                await Task.Delay(startupDelay);

                _logger.LogInformation("Validate welcome screen...");
                success = await _autoService.WaitForScreenAsync(
                    _atmScreens.First(s => s.Name.ToLower() == "welcome"),
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromSeconds(15));

                if (success == false)
                {
                    _logger.LogError("ATM not at welcome screen");
                    return false;
                }

                return true;
            }

            if (curScreen.Name.ToLower() != "welcome" && curScreen.Name.ToLower() != "outofservice")
            {
                return await DispatchToIdleAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "API connection failed");
            return false;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            await DispatchToIdleAsync();
        }
        finally
        {
            await _connectionService.CloseAsync();
            await _agentService.CloseSesisonAsync();
        }
    }

    public async Task<bool> DispatchToIdleAsync()
    {
        _logger.LogInformation("Dispatch to idle...");
        List<string> curScreen = await _autoService.GetScreenWordsAsync();
        int standardDelay = _config.GetValue<int>("Terminal:StandardDelayMS");

        if (curScreen is null)
        {
            _logger.LogError("Dispatch - Failed to read screen");
            return false;
        }
        else if (_autoService.MatchScreen(_atmScreens.First(s => s.Name.ToLower() == "welcome"), curScreen)
            || _autoService.MatchScreen(_atmScreens.First(s => s.Name.ToLower() == "outofservice"), curScreen))
        {
            _logger.LogInformation("Dispatch - ATM is idle");
            return true;
        }
        else if (_autoService.MatchScreen(_atmScreens.First(s => s.Name.ToLower() == "pleasewait"), curScreen))
        {
            _logger.LogInformation("Dispatch - Please wait");
            await Task.Delay(standardDelay);
            return await DispatchToIdleAsync();
        }
        else if (_autoService.MatchScreen(_atmScreens.First(s => s.Name.ToLower() == "moretime"), curScreen))
        {
            _logger.LogInformation("Dispatch - More time");
            var location = await _vmService.GetLocationByTextAsync("no");

            if (location is not null && location.Found)
            {
                await _vmService.ClickScreenAsync(new ClickScreenModel(location));
            }

            location = await _vmService.GetLocationByTextAsync("exit");

            if (location is not null && location.Found)
            {
                await _vmService.ClickScreenAsync(new ClickScreenModel(location));
            }

            location = await _vmService.GetLocationByTextAsync("return card");

            if (location is not null && location.Found)
            {
                await _vmService.ClickScreenAsync(new ClickScreenModel(location));
            }

            await Task.Delay(standardDelay);
            return await DispatchToIdleAsync();
        }
        else if (_autoService.MatchScreen(_atmScreens.First(s => s.Name.ToLower() == "anothertransaction"), curScreen))
        {
            _logger.LogInformation("Dispatch - Another transaction");
            var location = await _vmService.GetLocationByTextAsync("no");

            if (location is not null && location.Found)
            {
                await _vmService.ClickScreenAsync(new ClickScreenModel(location));
            }

            await Task.Delay(standardDelay);
            return await DispatchToIdleAsync();
        }
        else if (_autoService.MatchScreen(_atmScreens.First(s => s.Name.ToLower() == "takecard"), curScreen))
        {
            _logger.LogInformation("Dispatch - Take card");
            await TakeAllMediaAsync();
            await Task.Delay(standardDelay);
            return await DispatchToIdleAsync();
        }
        else if (_autoService.MatchScreen(_atmScreens.First(s => s.Name.ToLower() == "thankyou"), curScreen))
        {
            _logger.LogInformation("Dispatch - Thank you");
            await TakeAllMediaAsync();
            await Task.Delay(standardDelay);
            return await DispatchToIdleAsync();
        }
        else
        {
            _logger.LogInformation("Dispatch - Unrecognized screen");
            await Task.Delay(standardDelay);
            return await DispatchToIdleAsync();
        }
    }

    public async Task TakeAllMediaAsync()
    {
        await _atmService.TakeCardAsync();
        List<AtmServiceModel> services = await _atmService.GetServicesAsync();

        if (services is null)
        {
            return;
        }

        AtmServiceModel receiptPrinter = services.FirstOrDefault(x => x.DeviceType.ToLower() == "ptr");

        if (receiptPrinter is not null && receiptPrinter.Media > 0)
        {
            ReceiptModel receipt = await _atmService.TakeReceiptAsync(receiptPrinter.Name);

            if (receipt is not null)
            {
                string receiptText = JsonSerializer.Serialize(receipt.OcrData.Elements.ToList().Select(e => e.text));
                SaveReceiptAsync(_config["Preferences:DownloadPath"], receipt.result);
                _logger.LogInformation($"Take receipt -- {receiptText}");
            }
        }

        AtmServiceModel dispenser = services.FirstOrDefault(x => x.DeviceType.ToLower() == "cdm");

        if (dispenser is not null && dispenser.Media > 0)
        {
            AuditData auditData = await _atmService.TakeMediaAsync(dispenser.Name, dispenser.Media);

            if (auditData is not null)
            {
                _logger.LogInformation($"Take cash -- {JsonSerializer.Serialize(auditData)}");
            }
        }

        AtmServiceModel itemProcessor = services.FirstOrDefault(x => x.DeviceType.ToLower() == "ipm");

        if (itemProcessor is not null && itemProcessor.Media > 0)
        {
            AuditData auditData = await _atmService.TakeMediaAsync(itemProcessor.Name, itemProcessor.Media);

            if (auditData is not null)
            {
                _logger.LogInformation($"Take media -- {JsonSerializer.Serialize(auditData)}");
            }
        }
    }

    public async Task<bool> SaveScreenshotAsync(string folder)
    {
        try
        {
            string jpeg = await _vmService.GetScreenJpegAsync();

            if (string.IsNullOrWhiteSpace(jpeg) == false)
            {
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(
                    Path.Combine(folder, $"Screenshot-{DateTime.Now.ToString("yyyy-MM-dd--HH.mm.ss")}.jpg"),
                    Convert.FromBase64String(jpeg));
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public bool SaveReceiptAsync(string folder, string receiptJpeg)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(receiptJpeg) == false)
            {
                Directory.CreateDirectory(folder);
                File.WriteAllBytes(
                    Path.Combine(folder, $"Receipt-{DateTime.Now.ToString("yyyy-MM-dd--HH.mm.ss")}.jpg"),
                    Convert.FromBase64String(receiptJpeg));
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }
}

