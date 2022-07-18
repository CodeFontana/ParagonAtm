# Paragon Virtual ATM API Library
This library is used for interacting with a Paragon Virtual ATM via the published API.

[![Nuget Release](https://img.shields.io/nuget/v/CodeFoxtrot.ParagonAtmLibrary?style=for-the-badge)](https://www.nuget.org/packages/CodeFoxtrot.ParagonAtmLibrary/)

## Breaking Changes 1.8.5+
Support for multiple terminal profiles was added to Simulation.json. To accomplish this, a Preferences:SimulationProfile configuration parameter was added. See the sample Simulation.json below. The value of this parameter is how the library chooses which Terminal.{profile name} to choose for connection. If unspecified, the library will throw an exception.

## Breaking Changes 1.8.0+
The AvailableScreens.json was restructured, such that, rather than assigning a "Match Confidence" and "Edit Distance" to an entire ScreenModel, they are now per-phrase configuration. In this more granular approach, each phrase can be uniquely adjusted, rather than trying to find a one-size-fits-all setting for every phrase in that ScreenModel. See the sample below which reflects the new structure.

## How to use
Add latest **CodeFoxtrot.ParagonAtmLibrary** to your project via 'Manage Nuget Packages'... 

or via command line...

```
dotnet add package CodeFoxtrot.ParagonAtmLibrary --version 1.x.x
```

or via your .csproj file...

```
<PackageReference Include="CodeFoxtrot.ParagonAtmLibrary" Version="1.x.x" />
```

Add this to your ConfigureServices() method:

```
services.AddParagonAtmLibrary();
```

### Set your preferences -- Simulation.json
Specify your WebFast account, API endpoints if different from the defaults, a download path for storing screenshots, and the connection details for the Virtual ATM...

```
{
  "WebFast": {
    "Username": "bfontana",
    "Password": "",
    "GroupId": 8
  },
  "ApiEndpoint": {
    "Agent": "/api/v1/agent",
    "Connection": "/api/v1/connection",
    "VirtualMachine": "/api/v1/vm",
    "Atm": "/api/v1/atm",
    "WebFast": "/api/v1/wft"
  },
  "Preferences": {
    "SimulationProfile": "VMISED01",
    "DownloadPath": "C:\\Users\\Brian\\Desktop\\Screenshots"
  },
  "Terminal.VMISED01": {
    "Platform": "Edge",
    "Host": "http://10.44.112.155:13467",
    "HwProfile": "1744febb-dd28-433e-aa1b-33139c820d2e",
    "StartupApps": [
      "C:\\Users\\Public\\Desktop\\SSTAuto1.BAT"
    ],
    "StandardDelayMS": 1000,
    "StartupDelaySeconds": 420
  },
  "Terminal.VMISDB01": {
    "Platform": "Vista",
    "Host": "http://10.44.112.165:13467",
    "HwProfile": "debae18a-1308-4089-ad77-7eae88410753",
    "StartupApps": [
      "C:\\Phoenix\\bin\\mm99003.exe"
    ],
    "StandardDelayMS": 1000,
    "StartupDelaySeconds": 200
  }
}
```
Note: The "Terminal" configuration section is used by the demo to determine which Virtual ATM to connect with. In the above example, "Terminal.Vista" will be ignored, and you can rename the sections to swap which profile is the active one.

### Define your screen pool -- AvailableScreens.json
This JSON is used to define each screen. This is completely subjective, so you'll need to adjust accordingly with your testing.  

Each screen is given a name, and an array of phrases. Each phrase will have a corresponding 'MatchConfidence' and 'EditDistance' setting to help overcome minor inconsistencies with the OCR screen matching.  By supporting an array of phrases you can define multiple ways for matching that screen, e.g. cross-vendor phrasing or English vs Spanish language.

The **MatchConfidence** and **EditDistance** parameters are provided to accomodate variability with the Paragon screen OCR technology. OCR will not always read every word correctly from the screen.

**EditDistance** - Similar to spell check, specify the maximum acceptable character difference when comparing two words for equality. E.g. with an EditDistance=2, 'vout' would be considered matching with the word 'your'.

**MatchConfidence** - Specify the percentage of words from a given phrase that must match with the on-screen text, in order to declare a phrase as matching based on the on-screen text.

```
{
  "AvailableScreens": [
    {
      "Name": "Welcome",
      "Phrases": [
        {
          "Text": "Please insert your card",
          "MatchConfidence": 0.85,
          "EditDistance": 1
        },
        {
          "Text": "Por favor inserte su tarjeta",
          "MatchConfidence": 0.85,
          "EditDistance": 1
        },
        {
          "Text": "Please insert and remove your card",
          "MatchConfidence": 0.85,
          "EditDistance": 1
        },
        {
          "Text": "Por favor inserte y retire su tarjeta",
          "MatchConfidence": 0.85,
          "EditDistance": 1
        }
      ]
    },
    {
      "Name": "OutOfService",
      "Phrases": [
        {
          "Text": "This ATM is temporarily unavailable We're sorry for the inconvenience",
          "MatchConfidence": 0.80,
          "EditDistance": 1
        },
        {
          "Text": "Sorry this ATM is temporarily out of service",
          "MatchConfidence": 0.80,
          "EditDistance": 1
        },
        {
          "Text": "Sorry this machine is temporarily out of service",
          "MatchConfidence": 0.80,
          "EditDistance": 1
        }
      ]
    },
    ...
  ]
}  
```

### Code your transaction -- API and Automation methods
Using the Paragon-provided APIs and the additional automations provided by this library, inject any of the following services and consume their methods in whatever sequence works for what you are attempting to achieve!  

#### Paragon Built-in API methods

<ins>**IAgentService:**</ins>  
GetAgentStatusAsync(), GetUserGroupsAsync(), OpenHwProfileAsync(), StartAtmAppAsync(), OpenSesisonAsync(), CloseSesisonAsync()
  
<ins>**IConnectionService:**</ins>  
OpenAsync(), CloseAsync(), SaveCloseAsync(), SaveCloseRebootAsync(), CloseRebootAsync()
  
<ins>**IVirtualMachineService:**</ins>  
GetScreenJpegAsync(), GetScreenTextAsync(), ClickScreenAsync(), GetLocationByTextAsync()

<ins>**IAtmService:**</ins>  
GetServicesAsync(), GetDeviceStateAsync(), InsertCardAsync(), TakeCardAsync(), PressKeyAsync(), PressTtuKeyAsync(), GetPinpadKeysAsync(), ChangeOperatorSwitchAsync(), PushOperatorSwitchAsync(), EnterDieboldSupervisorModeAsync(), ExitDieboldSupervisorModeAsync(), OperatorSwitchStatusAsync(), InsertMediaAsync(), TakeMediaAsync(), TakeReceiptAsync(), RecoverAsync()
 
#### Library-provided API methods

<ins>**IClientService:**</ins>  
ConnectAsync(), DisconnectAsync(), DispatchToIdleAsync(), TakeAllMediaAsync(), SaveScreenshotAsync()

<ins>**IAutomationService:**</ins>  
CompareText() / CompareTextAsync(), FindAndClickAsync(), GetScreenWordsAsync(), MatchScreen() / MatchScreenAsync(), WaitForScreenAsync(), WaitForScreensAsync(), WaitForTextAsync()

### Demos
https://user-images.githubusercontent.com/41308769/179620502-1c16442a-c91e-46cf-a857-a1ead2e17f51.mp4  
https://user-images.githubusercontent.com/41308769/169855736-e5e9ca91-7a94-4335-83de-89fca32f3f6a.mp4
  
## More Information about Paragon Virtual ATM
https://www.paragonedge.com/products/virtualatm
