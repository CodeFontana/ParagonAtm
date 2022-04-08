# Paragon Virtual ATM API Library
This library is used for interacting with a Paragon Virtual ATM.  It provides the following services.

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

## Sample Implementation
https://github.com/CodeFontana/ParagonAtmClient

## More Information about Paragon Virtual ATM
https://www.paragonedge.com/products/virtualatm
