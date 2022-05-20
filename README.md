# Paragon Virtual ATM API Library
This library is used for interacting with a Paragon Virtual ATM via the published API.

[![Nuget Release](https://img.shields.io/nuget/v/CodeFoxtrot.ParagonAtmLibrary?style=for-the-badge)](https://www.nuget.org/packages/CodeFoxtrot.ParagonAtmLibrary/)

## How to use
Add latest **CodeFoxtrot.ParagonAtmLibrary** to your project via 'Manage Nuget Packages'... 

or via command line...

```
dotnet add package CodeFoxtrot.ParagonAtmLibrary --version 1.6.1
```

or via your .csproj file...

```
<PackageReference Include="CodeFoxtrot.ParagonAtmLibrary" Version="1.6.1" />
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
    "DownloadPath": "C:\\Users\\Brian\\Desktop\\Screenshots"
  },
  "Terminal": {
    "Host": "http://10.44.112.155:13467",
    "HwProfile": "1744febb-dd28-433e-aa1b-33139c820d2e",
    "StartupApps": [
      "C:\\Users\\Public\\Desktop\\SSTAuto1.BAT"
    ],
    "StandardDelayMS": 2000,
    "StartupDelaySeconds": 420
  },
  "Terminal.Vista": {
    "Host": "http://10.44.112.165:13467",
    "HwProfile": "e0d41618-e129-4c24-a626-319dbbb65572",
    "StartupApps": [
      "C:\\Phoenix\\bin\\mm99003.exe"
    ],
    "StandardDelayMS": 2000,
    "StartupDelaySeconds": 200
  }
}
```
Note: The "Terminal" configuration section is used by the demo to determine which Virtual ATM to connect with. In the above example, "Terminal.Vista" will be ignored, and you can rename the sections to swap which profile is the active one.

### Define your screen pool -- AvailableScreens.json
This JSON is used to define each screen. This is completely subjective, so you'll need to adjust accordingly with your testing.  

Each screen is given a name, an array of phrases, confidence level and acceptable edit distance.  The array of phrases allows you to define multiple potential values, e.g. English vs Spanish phrases.

The **MatchConfidence** and **EditDistance** parameters are provided to accomodate variability with the Paragon screen OCR technology. OCR will not always read every word correctly from the screen.

**EditDistance** - Similar to spell check, specify the maximum acceptable character difference when comparing two words for equality. E.g. with an EditDistance=2, 'vout' would be considered matching with the word 'your'.

**MatchConfidence** - Specify the percentage of words from a given phrase that must match with the on-screen text, in order to declare the phrase as matching based on the on-screen text.

```
"AvailableScreens": [
  {
    "Name": "Welcome",
    "Text": [
      "Please insert your card",
      "Por favor inserte su tarjeta",
      "Please insert and remove your card",
      "Por favor inserte y retire su tarjeta"
    ],
    "MatchConfidence": 0.80,
    "EditDistance": 2
  },
  {
    "Name": "OutOfService",
    "Text": [
      "This ATM is temporarily unavailable We're sorry for the inconvenience",
      "Sorry this ATM is temporarily out of service",
      "Sorry this machine is temporarily out of service"
    ],
    "MatchConfidence": 0.80,
    "EditDistance": 1
  },
    ... etc. 
  ]
}
```

### Code your transaction -- API method chart
Using the Paragon-provided APIs and the additional automations provided by this library, inject any of the following services and consume their methods in whatever sequence works for what you are attempting to achieve!  

<div align="center">
  
  #### Paragon Built-in API methods
  
  <table>
   <thead>
      <tr>
         <th><strong>IAgentService</strong></th>
         <th><strong>IConnectionService</strong></th>
         <th><strong>IVirtualMachineService</strong></th>
      </tr>
   </thead>
   <tbody>
      <tr>
         <td>GetAgentStatusAsync()</td>
         <td>OpenAsync()</td>
         <td>GetScreenJpegAsync()</td>
      </tr>
      <tr>
         <td>GetUserGroupsAsync()</td>
         <td>CloseAsync()</td>
         <td>GetScreenTextAsync()</td>
      </tr>
     <tr>
         <td>OpenHwProfileAsync()</td>
         <td>SaveCloseAsync()</td>
         <td>ClickScreenAsync()</td>
      </tr>
     <tr>
         <td>StartAtmAppAsync()</td>
         <td>SaveCloseRebootAsync()</td>
         <td>GetLocationByTextAsync()</td>
      </tr>
     <tr>
         <td>OpenSesisonAsync()</td>
         <td>CloseRebootAsync()</td>
         <td></td>
      </tr>
     <tr>
         <td>CloseSesisonAsync()</td>
         <td></td>
         <td></td>
      </tr>
   </tbody>
  </table>
  
    
  <table>
     <thead>
        <tr>
           <th><strong>IAtmService</strong></th>
           <th><strong>IAtmService (cont.)</strong></th>
           <th><strong>IAtmService (cont.)</strong></th>
        </tr>
     </thead>
     <tbody>
        <tr>
           <td>GetServicesAsync()</td>
           <td>GetDeviceStateAsync()</td>
           <td>InsertCardAsync()</td>
        </tr>
        <tr>
           <td>TakeCardAsync()</td>
           <td>PressKeyAsync()</td>
           <td>PressTtuKeyAsync()</td>
        </tr>
       <tr>
           <td>GetPinpadKeysAsync()</td>
           <td>ChangeOperatorSwitchAsync()</td>
           <td>PushOperatorSwitchAsync()</td>
        </tr>
       <tr>
           <td>EnterDieboldSupervisorModeAsync()</td>
           <td>ExitDieboldSupervisorModeAsync()</td>
           <td>OperatorSwitchStatusAsync()</td>
        </tr>
       <tr>
           <td>InsertMediaAsync()</td>
           <td>TakeMediaAsync()</td>
           <td>TakeReceiptAsync()</td>
        </tr>
       <tr>
           <td>RecoverAsync()</td>
           <td></td>
           <td></td>
        </tr>
     </tbody>
  </table>
  
    
  
  #### Library-provided API methods

  | IClientService         | IAutomationService
  |------------------------|------------------------------------|
  | ConnectAsync()         | CompareText() / CompareTextAsync() |
  | DisconnectAsync()      | FindAndClickAsync()                |
  | DispatchToIdleAsync()  | GetScreenWordsAsync()              |
  | TakeAllMediaAsync()    | MatchScreen() / MatchScreenAsync() |
  | SaveScreenshotsync()   | WaitForScreenAsync()               |
  |                        | WaitForScreensAsync()              |
  |                        | WaitForTextAsync()                 |
  
</div>

### Sample Balance Inquiry on an NCR Edge Virtual ATM

![2022-04-19_00-46-34](https://user-images.githubusercontent.com/41308769/163922140-478fb29d-81ca-4451-a8f6-dd7324f10f41.gif)

  
## More Information about Paragon Virtual ATM
https://www.paragonedge.com/products/virtualatm
