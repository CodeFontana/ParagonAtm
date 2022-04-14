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
            // Starting point -- InService/Welcome screen
            AtmScreenModel welcome = _atmScreens.First(s => s.Name.ToLower() == "inservice");
            bool atScreen = await _autoService.IsAtScreen(welcome);

            if (atScreen == false)
            {
                _logger.LogError("ATM not at welcome screen");
                return;
            }

            await _autoService.SaveScreenShot();

            // Get ATM services
            List<AtmServiceModel> services = await _atmService.GetServicesAsync();

            if (services is null)
            {
                _logger.LogError($"ATM service list is empty");
                return;
            }

            // Isolate card reader service
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

            // Instantiate card
            CardModel card = new(_config["Card:Id"], cardReader.Name);

            bool success = await _atmService.InsertCardAsync(card);

            if (success == false)
            {
                _logger.LogError("Failed to insert card for transaction");
                return;
            }
            
            // Validate -- Language selection screen
            atScreen = await _autoService.WaitForScreen(
                _atmScreens.First(s => s.Name.ToLower() == "language"), 
                TimeSpan.FromSeconds(30), 
                TimeSpan.FromSeconds(5));

            if (atScreen == false)
            {
                _logger.LogError($"ATM not at language screen");
                return;
            }

            await _autoService.SaveScreenShot();

            // Get location of transaction language
            LocationModel location = await _vmService.GetLocationByTextAsync(_config["Transaction:Language"]);

            if (location is null || location.Found == false)
            {
                _logger.LogError($"{_config["Transaction:Language"]} not found");
                return;
            }

            // Click language button
            success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

            if (success == false)
            {
                _logger.LogError("Failed to click english");
                return;
            }

            // Validate -- PIN screen
            atScreen = await _autoService.WaitForScreen(
                _atmScreens.First(s => s.Name.ToLower() == "pin"),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(5));

            if (atScreen == false)
            {
                _logger.LogError("Enter your PIN screen not found");
                return;
            }

            await _autoService.SaveScreenShot();

            // Isolate pinpad service
            AtmServiceModel pinpad = services?.FirstOrDefault(x => x.DeviceType.ToLower() == "pin");

            if (pinpad == null)
            {
                _logger.LogError($"Pinpad not found in device list");
                return;
            }

            // Type PIN
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

            // Validate -- Transaction type screen
            atScreen = await _autoService.WaitForScreen(
                _atmScreens.First(s => s.Name.ToLower() == "transactiontype"),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(5));

            if (atScreen == false)
            {
                _logger.LogError("Transaction type screen not found");
                return;
            }

            await _autoService.SaveScreenShot();

            // Find account balance button
            location = await _vmService.GetLocationByTextAsync("balance");

            if (location is null || location.Found == false)
            {
                _logger.LogError("Balance inquiry option not found");
                return;
            }

            // Click account balance button
            success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

            if (success == false)
            {
                _logger.LogError("Failed to click balance inquiry");
                return;
            }

            // Validate -- Account type screen
            atScreen = await _autoService.WaitForScreen(
                _atmScreens.First(s => s.Name.ToLower() == "accounttype"),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(5));

            if (atScreen == false)
            {
                _logger.LogError("Account type screen not found");
                return;
            }

            await _autoService.SaveScreenShot();

            // Find transaction account type
            location = await _vmService.GetLocationByTextAsync(_config["Transaction:AccountType"]);

            if (location is null || location.Found == false)
            {
                _logger.LogError($"{_config["Transaction:AccountType"]} account option not found");
                return;
            }

            // Click account type
            success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

            if (success == false)
            {
                _logger.LogError("Failed to click checking account");
                return;
            }

            // Validate -- Balance destination screen
            atScreen = await _autoService.WaitForScreen(
                _atmScreens.First(s => s.Name.ToLower() == "balancedestination"),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(5));

            if (atScreen == false)
            {
                _logger.LogError("Balance destination screen not found");
                return;
            }

            await _autoService.SaveScreenShot();

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

            // Validate -- Account name screen
            atScreen = await _autoService.WaitForScreen(
                _atmScreens.First(s => s.Name.ToLower() == "accountname"),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(5));

            if (atScreen == false)
            {
                _logger.LogError("Account name screen not found");
                return;
            }

            await _autoService.SaveScreenShot();

            // Find specified account button
            location = await _vmService.GetLocationByTextAsync(_config["Transaction:AccountName"]);

            if (location is null || location.Found == false)
            {
                _logger.LogError($"{_config["Transaction:AccountName"]} account not found");
                return;
            }

            // Click specified account button
            success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

            if (success == false)
            {
                _logger.LogError("Failed to click checking");
                return;
            }

            // Validate -- balance inquiry screen
            atScreen = await _autoService.WaitForScreen(
                _atmScreens.First(s => s.Name.ToLower() == "balanceinquiry"),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(5));

            if (atScreen == false)
            {
                _logger.LogError("Balance inquiry screen not found");
                return;
            }

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

            // Validate -- Another transaction screen
            atScreen = await _autoService.WaitForScreen(
                _atmScreens.First(s => s.Name.ToLower() == "anothertransaction"),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(5));

            if (atScreen == false)
            {
                _logger.LogError("Another transaction screen not found");
                return;
            }

            await _autoService.SaveScreenShot();

            // Find no button
            location = await _vmService.GetLocationByTextAsync("no");

            if (location is not null && location.Found)
            {
                // Click no button
                await _vmService.ClickScreenAsync(new ClickScreenModel(location));
            }

            // Validate -- Take card screen
            atScreen = await _autoService.WaitForScreen(
                _atmScreens.First(s => s.Name.ToLower() == "takecard"),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(5));

            if (atScreen == false)
            {
                _logger.LogError("Take card screen not found");
                return;
            }

            await _autoService.SaveScreenShot();
            await _atmService.TakeCardAsync();
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

                // Validate -- Welcome screen
                bool success = await _autoService.WaitForScreen(
                    _atmScreens.First(s => s.Name.ToLower() == "inservice"),
                    TimeSpan.FromMinutes(5),
                    TimeSpan.FromSeconds(15));

                if (success == false)
                {
                    _logger.LogError("ATM not at welcome screen");
                    return false;
                }

                return true;
            }

            // Ensure ATM is idle
            await DispatchToIdle();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, ex.Message);
            return false;
        }
    }
}
