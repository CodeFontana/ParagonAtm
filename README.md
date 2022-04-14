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

Add this to your ConfigureServices method:

```
services.AddParagonAtmLibrary();
```

Inject any of the following services...
* AgentService
* ConnectionService
* AtmService
* VirtualMachineService
* AutomationService

## Sample Implementation
https://github.com/CodeFontana/ParagonAtmClient

## More Information about Paragon Virtual ATM
https://www.paragonedge.com/products/virtualatm
