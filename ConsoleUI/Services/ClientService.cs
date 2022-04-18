using ConsoleUI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Interfaces;
using ParagonAtmLibrary.Models;

namespace ConsoleUI.Services;

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

        VirtualAtm = new()
        {
            Host = _config["Terminal:Host"],
            HwProfile = _config["Terminal:HwProfile"],
            StartupApp = _config["Terminal:AppStartup"]
        };

        WebFastUser = new WebFastUserModel
        {
            Username = _config["WebFast:Username"],
            Password = _config["WebFast:Password"],
            GroupId = int.Parse(_config["WebFast:GroupId"])
        };
    }

    public WebFastUserModel WebFastUser { get; set; }
    public TerminalModel VirtualAtm { get; set; }

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
                return false;
            }
            else if (agentState.AgentStatus.ToUpper().Equals("PAUSED"))
            {
                agentPaused = true;
            }

            if (await _agentService.OpenSesisonAsync(WebFastUser) == false)
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
                // Open hardware profile
                await _agentService.OpenHwProfileAsync(VirtualAtm);
            }

            await _connectionService.OpenAsync();

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
                await _agentService.StartAtmAppAsync(_config["Terminal:AppStartup"]);
                _logger.LogInformation("Delaying for 7 minutes while ATM app starts...");
                await Task.Delay(TimeSpan.FromMinutes(7));

                _logger.LogInformation("Validate welcome screen...");
                bool success = await _autoService.WaitForScreenAsync(
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
                return await DispatchToIdle();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "API connection failed");
            return false;
        }
    }

    public async Task<bool> DispatchToIdle()
    {
        _logger.LogInformation("Dispatch to idle...");
        List<string> curScreen = await _autoService.GetScreenWordsAsync();
        int standardDelay = _config.GetValue<int>("Terminal:StandardDelay");

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
            return await DispatchToIdle();
        }
        else if (_autoService.MatchScreen(_atmScreens.First(s => s.Name.ToLower() == "moretime"), curScreen))
        {
            _logger.LogInformation("Dispatch - More time");
            var location = await _vmService.GetLocationByTextAsync("no");

            if (location is not null && location.Found)
            {
                await _vmService.ClickScreenAsync(new ClickScreenModel(location));
            }

            await Task.Delay(standardDelay);
            return await DispatchToIdle();
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
            return await DispatchToIdle();
        }
        else if (_autoService.MatchScreen(_atmScreens.First(s => s.Name.ToLower() == "takecard"), curScreen))
        {
            _logger.LogInformation("Dispatch - Take card");
            await TakeAllMedia();
            await Task.Delay(standardDelay);
            return await DispatchToIdle();
        }
        else
        {
            _logger.LogInformation("Dispatch - Unrecognized screen");
            await Task.Delay(standardDelay);
            await DispatchToIdle();
        }
    }

    public async Task<bool> SaveScreenShot(string folder)
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
}
