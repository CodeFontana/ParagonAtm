# Paragon Virtual ATM API Library
This library is used for interacting with a Paragon Virtual ATM via the published API.

[![Nuget Release](https://img.shields.io/nuget/v/CodeFoxtrot.ParagonAtmLibrary?style=for-the-badge)](https://www.nuget.org/packages/CodeFoxtrot.ParagonAtmLibrary/)

## How to use
Add latest CodeFoxtrot.ParagonAtmLibrary to your project via 'Manage Nuget Packages' or your favorite method...

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
    "Password": "<I would use user secrets for this>",
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
  
```
public class ClientApp : IHostedService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly IConfiguration _config;
    private readonly ILogger<ClientApp> _logger;
    private readonly AgentService _agentService;
    private readonly ConnectionService _connectionService;
    private readonly VirtualMachineService _virtualMachineService;
    private readonly AtmService _atmService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ClientApp(IHostApplicationLifetime hostApplicationLifetime,
                     IConfiguration configuration,
                     ILogger<ClientApp> logger,
                     AgentService agentService,
                     ConnectionService connectionService,
                     VirtualMachineService virtualMachineService,
                     AtmService atmService)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _config = configuration;
        _logger = logger;
        _agentService = agentService;
        _connectionService = connectionService;
        _virtualMachineService = virtualMachineService;
        _atmService = atmService;
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
    bool sessionOpened = false;

    try
    {
        AgentStatusModel agentState = await _agentService.GetAgentStatusAsync();

        if (agentState == null)
        {
            _logger.LogError("Unable to query agent status, may not be running, unable to acquire session");
            return;
        }
        else if (agentState.AgentStatus.ToUpper().Equals("IDLE") == false
            && agentState.AgentStatus.ToUpper().Equals("PAUSED") == false)
        {
            _logger.LogError("Agent must be in IDLE or PAUSED state to acquire session");
            return;
        }

        sessionOpened = await _agentService.OpenSesisonAsync(WebFastUser);

        agentState = await _agentService.GetAgentStatusAsync();

        if (sessionOpened == false)
        {
            _logger.LogError("Failed to acquire session");
            return;
        }
        else if (agentState.AgentStatus.ToUpper().Equals("APICONTROLLED") == false)
        {
            _logger.LogError($"Unexpected session state [{agentState.AgentStatus}], expecting [APICONTROLLED]");
            await _agentService.CloseSesisonAsync();
            return;
        }

        await _agentService.OpenHwProfileAsync(VirtualAtm);
        await _connectionService.OpenAsync();

        // TODO: Add your automation instructions here!

    }
    catch (Exception ex)
    {
        _logger.LogCritical(ex, ex.Message);
    }
    finally
    {
        if (sessionOpened)
        {
            await _agentService.CloseSesisonAsync();
        }
    }
}
```

## Sample Implementation
https://github.com/CodeFontana/ParagonAtmClient

## More Information about Paragon Virtual ATM
https://www.paragonedge.com/products/virtualatm
