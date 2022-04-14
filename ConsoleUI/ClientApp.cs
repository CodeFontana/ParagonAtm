using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Models;
using ParagonAtmLibrary.Services;
using System.Text.Json;

namespace VirtualAtmClient;

public class ClientApp : IHostedService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IConfiguration _config;
    private readonly ILogger<ClientApp> _logger;
    private readonly HttpClient _httpClient;
    private readonly AgentService _agentService;
    private readonly ConnectionService _connectionService;
    private readonly VirtualMachineService _vmService;
    private readonly AtmService _atmService;
    private readonly AutomationService _autoService;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly List<AtmScreenModel> _atmScreens;

    public ClientApp(IHostApplicationLifetime hostApplicationLifetime,
                     IConfiguration configuration,
                     ILogger<ClientApp> logger,
                     HttpClient httpClient,
                     AgentService agentService,
                     ConnectionService connectionService,
                     VirtualMachineService vmService,
                     AtmService atmService,
                     AutomationService autoService)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _config = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _agentService = agentService;
        _connectionService = connectionService;
        _vmService = vmService;
        _atmService = atmService;
        _autoService = autoService;
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        _atmScreens = _config.GetSection("Screens").Get<List<AtmScreenModel>>();

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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _hostApplicationLifetime.ApplicationStarted.Register(async () =>
        {
            try
            {
                await Task.Yield(); // https://github.com/dotnet/runtime/issues/36063
                await Task.Delay(1000); // Additional delay for Microsoft.Hosting.Lifetime messages
                await ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception!");
            }
            finally
            {
                _hostApplicationLifetime.StopApplication();
            }
        });

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await DispatchToIdle();
        await _connectionService.CloseAsync();
        await _agentService.CloseSesisonAsync();
    }

    public async Task ExecuteAsync()
    {
        try
        {
            bool sessionOpened = await ConnectAsync();

            if (sessionOpened == false)
            {
                return;
            }

            await BalanceInquiry();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, ex.Message);
        }
    }

    public async Task BalanceInquiry()
    {
        try
        {
            // Wait for Welcome screen
            bool foundText = await _autoService.WaitForText("Welcome", TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1));

            if (foundText)
            {
                await _autoService.SaveScreenShot();
            }
            else
            {
                _logger.LogError("ATM not at welcome screen");
                return;
            }

            // Get ATM services
            var services = await _atmService.GetServicesAsync();

            if (services is null)
            {
                _logger.LogError($"ATM service list is empty");
                return;
            }

            // Find card reader
            var cardReader = services?.FirstOrDefault(x => x.DeviceType.ToLower() == "idc");

            if (cardReader == null)
            {
                _logger.LogError($"Card reader not found in device list");
                return;
            }
            else if (cardReader.IsOpen == false)
            {
                _logger.LogError($"Card reader is not open");
                return;
            }

            // Get card for transaction
            var glass2 = new CardModel(_config["Card:Id"], cardReader.Name);

            // Insert card
            bool success = await _atmService.InsertCardAsync(glass2);

            if (success == false)
            {
                _logger.LogError("Failed to insert card for transaction");
                return;
            }
            else
            {
                await Task.Delay(2000);
                await _autoService.SaveScreenShot();
            }

            // Wait for language selection
            foundText = await _autoService.WaitForText("language", TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));

            if (foundText == false)
            {
                return;
            }

            // Find English button
            var location = await _vmService.GetLocationByTextAsync("English");

            if (location is null || location.Found == false)
            {
                _logger.LogError("English not found");
                return;
            }

            // Click English button
            success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

            if (success == false)
            {
                _logger.LogError("Failed to click english");
                return;
            }
            else
            {
                await Task.Delay(2000);
                await _autoService.SaveScreenShot();
            }

            // Wait for Enter your PIN screen
            foundText = await _autoService.WaitForText(new[] { "Personal", "Identification", "Number" }, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5), true);

            if (foundText == false)
            {
                _logger.LogError("Enter your PIN screen not found");
                return;
            }

            // Find pinpad
            var pinpad = services?.FirstOrDefault(x => x.DeviceType.ToLower() == "pin");

            if (pinpad == null)
            {
                _logger.LogError($"Pinpad not found in device list");
                return;
            }

            // Enter card pin
            foreach (char c in _config["Card:Pin"])
            {
                success = await _atmService.PressKeyAsync(new PressKeyModel(pinpad.Name, $"n{c}"));

                if (success == false)
                {
                    _logger.LogError($"Failed to press pinpad key -- {c}");
                    return;
                }

                await Task.Delay(1000);
            }

            await _autoService.SaveScreenShot();

            // Press ENTER
            success = await _atmService.PressKeyAsync(new PressKeyModel(pinpad.Name, "Enter"));

            if (success == false)
            {
                _logger.LogError("Failed to press pinpad key -- Enter");
                return;
            }
            else
            {
                await Task.Delay(5000);
                await _autoService.SaveScreenShot();
            }

            // Find balance inquiry button
            location = await _vmService.GetLocationByTextAsync("balance");

            if (location is null || location.Found == false)
            {
                _logger.LogError("Balance inquiry option not found");
                return;
            }

            // Click balance inquiry button
            success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

            if (success == false)
            {
                _logger.LogError("Failed to click balance inquiry");
                return;
            }
            else
            {
                await Task.Delay(2000);
                await _autoService.SaveScreenShot();
            }

            // Find checking account button
            location = await _vmService.GetLocationByTextAsync("checking");

            if (location is null || location.Found == false)
            {
                _logger.LogError("Checking account option not found");
                return;
            }

            // Click checking account button
            success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

            if (success == false)
            {
                _logger.LogError("Failed to click checking account");
                return;
            }
            else
            {
                await Task.Delay(2000);
                await _autoService.SaveScreenShot();
            }

            // Find display balance button
            location = await _vmService.GetLocationByTextAsync("display balance");

            if (location is null || location.Found == false)
            {
                _logger.LogError("display balance option not found");
                return;
            }

            // Click display balance button
            success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

            if (success == false)
            {
                _logger.LogError("Failed to click display balance");
                return;
            }
            else
            {
                await Task.Delay(2000);
                await _autoService.SaveScreenShot();
            }

            await _autoService.WaitForText(new[] { "desired", "account" }, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(5));
            await _autoService.SaveScreenShot();

            // Find checking account button
            location = await _vmService.GetLocationByTextAsync("checking|t");

            if (location is null || location.Found == false)
            {
                _logger.LogError("Checking account not found");
                return;
            }

            // Click checking button
            success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

            if (success == false)
            {
                _logger.LogError("Failed to click checking");
                return;
            }
            else
            {
                await Task.Delay(2000);
                await _autoService.SaveScreenShot();
            }

            await _autoService.WaitForText(new[] { "Total", "Balance" }, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(5));
            await _autoService.SaveScreenShot();

            // Find continue button
            location = await _vmService.GetLocationByTextAsync("continue");

            if (location is null || location.Found == false)
            {
                _logger.LogError("Continue not found");
                return;
            }

            // Click continue button
            success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

            if (success == false)
            {
                _logger.LogError("Failed to click continue");
                return;
            }
            else
            {
                await Task.Delay(2000);
                await _autoService.SaveScreenShot();
            }

            // Another transaction?
            if (await _autoService.SearchForText(new[] { "another", "transaction" }))
            {
                // Find no button
                location = await _vmService.GetLocationByTextAsync("no");

                if (location is not null && location.Found)
                {
                    // Click no button
                    await _vmService.ClickScreenAsync(new ClickScreenModel(location));
                    await Task.Delay(5000);
                }
            }

            // Take your card
            if (await _autoService.SearchForText(new[] { "take", "card" }))
            {
                await _atmService.TakeCardAsync();
                await Task.Delay(10000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Balance inquiry failed");
        }
    }

    public async Task DispatchToIdle()
    {
        List<string> curScreen = await _autoService.GetScreenWords();

        if (curScreen is null)
        {
            _logger.LogError("Dispatch - Failed to read screen");
            return;
        }
        if (_autoService.MatchScreen(curScreen, new List<string> { "recycle bin" }, 0.50M))
        {
            _logger.LogInformation("Dispatch - ATM is at desktop");
            return;
        }
        else if (_autoService.MatchScreen(curScreen, _atmScreens.First(s => s.Name.ToLower() == "inservice").Text, 0.50M)
            || _autoService.MatchScreen(curScreen, _atmScreens.First(s => s.Name.ToLower() == "outservice").Text, 0.50M))
        {
            _logger.LogInformation("Dispatch - ATM is idle");
            return;
        }
        else if (_autoService.MatchScreen(curScreen, _atmScreens.First(s => s.Name.ToLower() == "pleasewait").Text, 0.50M))
        {
            _logger.LogInformation("Dispatch - Please wait");
            await Task.Delay(5000);
            await DispatchToIdle();
            return;
        }
        else if (_autoService.MatchScreen(curScreen, _atmScreens.First(s => s.Name.ToLower() == "moretime").Text, 0.50M))
        {
            _logger.LogInformation("Dispatch - More time");

            var location = await _vmService.GetLocationByTextAsync("no");

            if (location is not null && location.Found)
            {
                await _vmService.ClickScreenAsync(new ClickScreenModel(location));
                await Task.Delay(5000);
            }

            await DispatchToIdle();
            return;
        }
        else if (_autoService.MatchScreen(curScreen, _atmScreens.First(s => s.Name.ToLower() == "anothertransaction").Text, 0.50M))
        {
            _logger.LogInformation("Dispatch - Another transaction");

            var location = await _vmService.GetLocationByTextAsync("no");

            if (location is not null && location.Found)
            {
                await _vmService.ClickScreenAsync(new ClickScreenModel(location));
                await Task.Delay(5000);
            }

            await DispatchToIdle();
            return;
        }
        else if (_autoService.MatchScreen(curScreen, _atmScreens.First(s => s.Name.ToLower() == "takecard").Text, 0.50M))
        {
            _logger.LogInformation("Dispatch - Take card");
            await _atmService.TakeCardAsync();
            await Task.Delay(10000);
            await DispatchToIdle();
            return;
        }
    }

    public async Task<bool> ConnectAsync()
    {
        try
        {
            // Get agent status
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

            // Open session
            if (await _agentService.OpenSesisonAsync(WebFastUser) == false)
            {
                _logger.LogError("Failed to acquire session");
                return false;
            }

            // Verify state is now -- APICONTROLLED
            agentState = await _agentService.GetAgentStatusAsync();

            if (agentState.AgentStatus.ToUpper().Equals("APICONTROLLED") == false)
            {
                _logger.LogError($"Unexpected session state [{agentState.AgentStatus}], expecting [APICONTROLLED]");
                return false;
            }

            if (agentPaused == false)
            {
                // Open hardware profile and connect to API
                await _agentService.OpenHwProfileAsync(VirtualAtm);
            }

            await _connectionService.OpenAsync();

            // Use OCR to figure out if ATM app is already running
            bool appRunning = await _autoService.IsAtScreen(_atmScreens);

            if (appRunning == false)
            {
                // Start ATM app
                await _agentService.StartAtmAppAsync(_config["Terminal:AppStartup"]);
                _logger.LogInformation("Delaying for 7 minutes while ATM app starts...");
                await Task.Delay(TimeSpan.FromMinutes(7));

                // Wait and verify at Welcome screen
                bool success = await _autoService.WaitForText("Welcome", TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(15));

                if (success == false)
                {
                    _logger.LogError("ATM not at welcome screen");
                    return false;
                }

                return true;
            }

            // Recover from prior transaction
            await DispatchToIdle();

            // Check for welcome screen
            if (await _autoService.IsAtScreen(_atmScreens.First(s => s.Name.ToLower() == "inservice"), 0.50M) == false)
            {
                _logger.LogError("ATM not at welcome screen");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, ex.Message);
            return false;
        }
    }
}
