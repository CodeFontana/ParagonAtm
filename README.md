# Paragon Virtual ATM API Library
This library is used for interacting with a Paragon Virtual ATM via the published API.

[![Nuget Release](https://img.shields.io/nuget/v/CodeFoxtrot.ParagonAtmLibrary?style=for-the-badge)](https://www.nuget.org/packages/CodeFoxtrot.ParagonAtmLibrary/)

## How to use
Add latest **CodeFoxtrot.ParagonAtmLibrary** to your project via 'Manage Nuget Packages'... 

or via command line...

```
dotnet add package CodeFoxtrot.ParagonAtmLibrary --version 1.0.1
```

or via your .csproj file...

```
<PackageReference Include="CodeFoxtrot.ParagonAtmLibrary" Version="1.0.1" />
```

Add the following to your appsettings.json, and be sure to update relevant feilds. I recommend taking advantage of 'Manage User Secrets' for your Visual Studio project, for storing your WebFast server credentials:
```
"WebFast": {
    "Username": "bfontana",
    "Password": "<User Secret>",
    "GroupId": 8
  },
  "ApiEndpoint": {
    "Agent": "/api/v1/agent",
    "Connection": "/api/v1/connection",
    "VirtualMachine": "/api/v1/vm",
    "Atm": "/api/v1/atm",
    "WebFast": "/api/v1/wft"
  },
  "Terminal": {
    "Host": "http://10.44.112.155:13467",
    "HwProfile": "1744febb-dd28-433e-aa1b-33139c820d2e",
    "AppStartup": "C:\\Users\\Public\\Desktop\\SSTAuto1.BAT"
  },
  "Card": {
    "Id": "f2305283-bb84-49fe-aba6-cd3f7bcfa5ba",
    "Pin": "1234"
  },
  "Preferences": {
    "DownloadPath": "C:\\Users\\Brian\\Desktop"
  }
```

Add this to your ConfigureServices method:

```
services.AddParagonAtmLibrary();
```

Inject any of the following services...
* AgentService
* ConnectionService
* AtmService
* VirtualMachineService
* AUtomationService
  
```
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

...

}
```

Use the services...

```
public async Task ExecuteAsync()
{
    try
    {
        bool sessionOpened = await ConnectAsync();

        if (sessionOpened == false)
        {
            return;
        }

        // TODO: Add your automation instructions here!
    }
    catch (Exception ex)
    {
        _logger.LogCritical(ex, ex.Message);
    }
    finally
    {
        // Cleanup...
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
        bool appRunning = await _autoService.SearchForWords(new[] { "welcome", "terminal", "insert", "card", "more", "time" }, false);

        if (appRunning == false)
        {
            // Start ATM app
            await _agentService.StartAtmAppAsync(_config["Terminal:AppStartup"]);
            _logger.LogInformation("Delaying for 8 minutes while ATM app starts...");
            await Task.Delay(TimeSpan.FromMinutes(8));

            // Wait and verify at Welcome screen
            bool success = await WaitForWelcomeScreen(TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(15));

            if (success == false)
            {
                _logger.LogError("ATM not at welcome screen");
                return false;
            }

            return true;
        }

        // Recover from prior transaction
        await RecoverBadTransaction();

        // Check for welcome screen
        if (await IsWelcomeScreen() == false)
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

public async Task<bool> IsWelcomeScreen()
{
    bool isWelcomeScreen = await _autoService.SearchForWords(new[] { "welcome" });

    if (isWelcomeScreen)
    {
        return true;
    }
    else
    {
        return false;
    }
}

public async Task<bool> WaitForWelcomeScreen(TimeSpan timeout, TimeSpan interval)
{
    bool foundText = await _autoService.WaitForScreenText("Welcome", timeout, interval);

    if (foundText == false)
    {
        return false;
    }
    else
    {
        return true;
    }
}
```

## Sample Implementation
https://github.com/CodeFontana/ParagonAtmClient

## More Information about Paragon Virtual ATM
https://www.paragonedge.com/products/virtualatm
