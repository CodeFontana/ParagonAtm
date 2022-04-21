# Paragon Virtual ATM API Library
This library is used for interacting with a Paragon Virtual ATM via the published API.

[![Nuget Release](https://img.shields.io/nuget/v/CodeFoxtrot.ParagonAtmLibrary?style=for-the-badge)](https://www.nuget.org/packages/CodeFoxtrot.ParagonAtmLibrary/)

## How to use
Add latest **CodeFoxtrot.ParagonAtmLibrary** to your project via 'Manage Nuget Packages'... 

or via command line...

```
dotnet add package CodeFoxtrot.ParagonAtmLibrary --version 1.5.6
```

or via your .csproj file...

```
<PackageReference Include="CodeFoxtrot.ParagonAtmLibrary" Version="1.5.6" />
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
    "StandardDelayMS": 5000,
    "StartupDelaySeconds": 420
  },
  "Terminal.Vista": {
    "Host": "http://10.44.112.165:13467",
    "HwProfile": "e0d41618-e129-4c24-a626-319dbbb65572",
    "StartupApps": [
      "C:\\Phoenix\\bin\\mm99003.exe"
    ],
    "StandardDelay": 5000,
    "StartupDelaySeconds": 200
  }
}
```
Note: The "Terminal" configuration section is used by the demo to determine which Virtual ATM to connect with. In the above example, "Terminal.Vista" will be ignored, and you can rename the sections to swap which profile is the active one.

### Define your screen pool -- AvailableScreens.json
This JSON is used to define each screen. This is completely subjective, so you'll need to adjust accordingly with your testing.  

Each screen is given a name, an array of phrases and a confidence level.  The array of phrases allows you to define multiple potential values, e.g. English vs Spanish phrases.  Or you might have different scenarios with different text.  

The **MatchConfidence** is provided to accomodate variability with the Paragon screen OCR technology. Not always will OCR read every word correctly, so how much of specific phrase do you require to match, in order to say the current ATM screen is a match for what you've defined here...  

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
    "MatchConfidence": 0.80
  },
  {
    "Name": "OutOfService",
    "Text": [
      "This ATM is temporarily unavailable We're sorry for the inconvenience",
      "Sorry this ATM is temporarily out of service",
      "Sorry this machine is temporarily out of service"
    ],
    "MatchConfidence": 0.80
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
  |                        | WaitForTextAsync()                 |
  
</div>

### Sample Balance Inquiry on an NCR Edge Virtual ATM -- EdgeConsumerTransactionService.cs
Note this is totally dependent on your ATMs screen flow!  

```
public async Task BalanceInquiry(string cardId = "f2305283-bb84-49fe-aba6-cd3f7bcfa5ba",
                                 string cardPin = "1234",
                                 string language = "English",
                                 string accountType = "Checking",
                                 string accountName = "Checking|T",
                                 string receiptOption = "Print and Display")
{
    try
    {
        string saveFolder = Path.Combine(_config["Preferences:DownloadPath"], DateTime.Now.ToString("yyyy-MM-dd--HH.mm.ss"));
        string transactionType = "Account Balance";

        if (language.ToLower() == "espanol")
        {
            transactionType = "Saldos de Cuenta";
        }

        // Starting point -- InService/Welcome screen
        AtmScreenModel welcomeScreen = _atmScreens.First(s => s.Name.ToLower() == "welcome");
        bool atScreen = await _autoService.MatchScreenAsync(welcomeScreen);

        if (atScreen == false)
        {
            _logger.LogError("ATM not at welcome screen");
            return;
        }

        await _clientService.SaveScreenshotAsync(saveFolder);

        // Get ATM services
        List<AtmServiceModel> services = await _atmService.GetServicesAsync();

        if (services is null)
        {
            _logger.LogError($"ATM service list is empty");
            return;
        }

        // Isolate card reader service
        AtmServiceModel cardReader = services?.FirstOrDefault(x => x.DeviceType.ToLower() == "idc");

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

        // Insert specified card
        CardModel card = new(cardId, cardReader.Name);
        bool success = await _atmService.InsertCardAsync(card);

        if (success == false)
        {
            _logger.LogError("Failed to insert card for transaction");
            return;
        }

        int standardDelay = 5000;
        await Task.Delay(standardDelay);

        // Validate -- Language selection screen
        AtmScreenModel languageScreen = _atmScreens.First(s => s.Name.ToLower() == "languageselection");
        atScreen = await _autoService.WaitForScreenAsync(languageScreen,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5));

        if (atScreen == false)
        {
            _logger.LogError($"ATM not at language screen");
            return;
        }

        await _clientService.SaveScreenshotAsync(saveFolder);

        if (await _autoService.FindAndClickAsync(language) == false)
        {
            _logger.LogError($"Failed to find and click '{language}' button");
            return;
        }

        await Task.Delay(standardDelay);

        // Validate -- PIN screen
        atScreen = await _autoService.WaitForScreenAsync(
            _atmScreens.First(s => s.Name.ToLower() == "pin"),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5));

        if (atScreen == false)
        {
            _logger.LogError("Enter your PIN screen not found");
            return;
        }

        await _clientService.SaveScreenshotAsync(saveFolder);

        // Isolate pinpad service
        AtmServiceModel pinpad = services?.FirstOrDefault(x => x.DeviceType.ToLower() == "pin");

        if (pinpad == null)
        {
            _logger.LogError($"Pinpad not found in device list");
            return;
        }

        // Type PIN
        foreach (char c in cardPin)
        {
            success = await _atmService.PressKeyAsync(new PressKeyModel(pinpad.Name, $"n{c}"));

            if (success == false)
            {
                _logger.LogError($"Failed to press pinpad key -- {c}");
                return;
            }

            await Task.Delay(1000);
        }

        await _clientService.SaveScreenshotAsync(saveFolder);

        // Press ENTER
        success = await _atmService.PressKeyAsync(new PressKeyModel(pinpad.Name, "Enter"));

        if (success == false)
        {
            _logger.LogError("Failed to press pinpad key -- Enter");
            return;
        }

        await Task.Delay(standardDelay);

        // Validate -- Transaction type screen
        atScreen = await _autoService.WaitForScreenAsync(
            _atmScreens.First(s => s.Name.ToLower() == "transactiontype"),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5));

        if (atScreen == false)
        {
            _logger.LogError("Transaction type screen not found");
            return;
        }

        await _clientService.SaveScreenshotAsync(saveFolder);

        if (await _autoService.FindAndClickAsync(transactionType) == false)
        {
            _logger.LogError($"Failed to find and click '{transactionType}' button");
            return;
        }

        await Task.Delay(standardDelay);

        // Validate -- Account type screen
        atScreen = await _autoService.WaitForScreenAsync(
            _atmScreens.First(s => s.Name.ToLower() == "accounttype"),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5));

        if (atScreen == false)
        {
            _logger.LogError("Account type screen not found");
            return;
        }

        await _clientService.SaveScreenshotAsync(saveFolder);

        if (await _autoService.FindAndClickAsync(accountType) == false)
        {
            _logger.LogError($"Failed to find and click '{accountType}' button");
            return;
        }

        await Task.Delay(standardDelay);

        // Validate -- Balance destination screen
        atScreen = await _autoService.WaitForScreenAsync(
            _atmScreens.First(s => s.Name.ToLower() == "balancedestination"),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5));

        if (atScreen == false)
        {
            _logger.LogError("Balance destination screen not found");
            return;
        }

        await _clientService.SaveScreenshotAsync(saveFolder);

        if (await _autoService.FindAndClickAsync(receiptOption) == false)
        {
            _logger.LogError($"Failed to find and click '{receiptOption}' button");
            return;
        }

        await Task.Delay(standardDelay);

        // Validate -- Account name screen
        atScreen = await _autoService.WaitForScreenAsync(
            _atmScreens.First(s => s.Name.ToLower() == "accountname"),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5));

        if (atScreen == false)
        {
            _logger.LogError("Account name screen not found");
            return;
        }

        await _clientService.SaveScreenshotAsync(saveFolder);

        if (await _autoService.FindAndClickAsync(accountName) == false)
        {
            _logger.LogError($"Failed to find and click '{accountName}' button");
            return;
        }

        await Task.Delay(standardDelay);

        // Validate -- balance inquiry screen
        atScreen = await _autoService.WaitForScreenAsync(
            _atmScreens.First(s => s.Name.ToLower() == "balanceinquiry"),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5));

        if (atScreen == false)
        {
            _logger.LogError("Balance inquiry screen not found");
            return;
        }

        await _clientService.SaveScreenshotAsync(saveFolder);

        if (await _autoService.FindAndClickAsync(new string[] { "continue", "continuar" }) == false)
        {
            _logger.LogError($"Failed to find and click 'Continue' or 'Continuar' button");
            return;
        }

        await Task.Delay(standardDelay);

        // Isolate receipt printer service
        AtmServiceModel receiptPrinter = services?.FirstOrDefault(x => x.DeviceType.ToLower() == "ptr");

        if (receiptPrinter == null)
        {
            _logger.LogError($"Receipt printer not found in device list");
            return;
        }
        else if (receiptPrinter.IsOpen == false)
        {
            _logger.LogError($"Receipt printer is not open");
            return;
        }

        // Take receipt
        ReceiptModel receipt = await _atmService.TakeReceiptAsync(receiptPrinter.Name, saveFolder);

        if (receipt is not null)
        {
            string receiptText = JsonSerializer.Serialize(receipt.OcrData.Elements.ToList().Select(e => e.text));
            _clientService.SaveReceiptAsync(saveFolder, receipt.result);
            _logger.LogInformation($"Take receipt -- {receiptText}");
        }

        // Validate -- Another transaction screen
        atScreen = await _autoService.WaitForScreenAsync(
            _atmScreens.First(s => s.Name.ToLower() == "anothertransaction"),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5));

        if (atScreen == false)
        {
            _logger.LogError("Another transaction screen not found");
            return;
        }

        await _clientService.SaveScreenshotAsync(saveFolder);

        if (await _autoService.FindAndClickAsync("no") == false)
        {
            _logger.LogError($"Failed to find and click 'No' button");
            return;
        }

        await Task.Delay(standardDelay);

        // Validate -- Take card screen
        atScreen = await _autoService.WaitForScreenAsync(
            _atmScreens.First(s => s.Name.ToLower() == "takecard"),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(5));

        if (atScreen == false)
        {
            _logger.LogError("Take card screen not found");
            return;
        }

        await _clientService.SaveScreenshotAsync(saveFolder);
        await _atmService.TakeCardAsync();
        await Task.Delay(standardDelay);
        await _clientService.SaveScreenshotAsync(saveFolder);
    }
    catch (Exception ex)
    {
        _logger.LogCritical(ex, "Balance inquiry failed");
    }
}
```
#### And it goes like this...

![2022-04-19_00-46-34](https://user-images.githubusercontent.com/41308769/163922140-478fb29d-81ca-4451-a8f6-dd7324f10f41.gif)


## More Information about Paragon Virtual ATM
https://www.paragonedge.com/products/virtualatm
