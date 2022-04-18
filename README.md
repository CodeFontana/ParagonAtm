# Paragon Virtual ATM API Library
This library is used for interacting with a Paragon Virtual ATM via the published API.

[![Nuget Release](https://img.shields.io/nuget/v/CodeFoxtrot.ParagonAtmLibrary?style=for-the-badge)](https://www.nuget.org/packages/CodeFoxtrot.ParagonAtmLibrary/)

## How to use
Add latest **CodeFoxtrot.ParagonAtmLibrary** to your project via 'Manage Nuget Packages'... 

or via command line...

```
dotnet add package CodeFoxtrot.ParagonAtmLibrary --version 1.4.6
```

or via your .csproj file...

```
<PackageReference Include="CodeFoxtrot.ParagonAtmLibrary" Version="1.4.6" />
```

Add this to your ConfigureServices() method:

```
services.AddParagonAtmLibrary();
```

Inject any of the following services to consume their methods...
* **ClientService**
  + ConnectAsync()
  + DisconnectAsync()
  + DispatchToIdleAsync()
  + TakeAllMediaAsync()
  + SaveScreenshotsync()
* **AgentService**
  + GetAgentStatusAsync()
  + GetUserGroupsAsync()
  + OpenHwProfileAsync()
  + StartAtmAppAsync()
  + OpenSesisonAsync()
  + CloseSesisonAsync()
* **ConnectionService**
  + OpenAsync()
  + CloseAsync()
  + SaveCloseAsync()
  + SaveCloseRebootAsync()
  + CloseRebootAsync()
* **AtmService**
  + GetServicesAsync()
  + GetDeviceStateAsync()
  + InsertCardAsync()
  + TakeCardAsync()
  + PressKeyAsync()
  + PressTtuKeyAsync()
  + GetPinpadKeysAsync()
  + ChangeOperatorSwitchAsync()
  + PushOperatorSwitchAsync()
  + EnterDieboldSupervisorModeAsync()
  + ExitDieboldSupervisorModeAsync()
  + OperatorSwitchStatusAsync()
  + InsertMediaAsync()
  + TakeMediaAsync()
  + TakeReceiptAsync()
  + RecoverAsync()
* **VirtualMachineService**
  + GetScreenJpegAsync()
  + GetScreenTextAsync()
  + ClickScreenAsync()
  + GetLocationByTextAsync()
* **AutomationService**
  + CompareText()
  + CompareTextAsync()
  + GetScreenWordsAsync()
  + MatchScreen()
  + MatchScreenAsync()
  + WaitForScreenAsync()
  + WaitForTextAsync()

## More Information about Paragon Virtual ATM
https://www.paragonedge.com/products/virtualatm
