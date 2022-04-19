# Paragon Virtual ATM API Library
This library is used for interacting with a Paragon Virtual ATM via the published API.

[![Nuget Release](https://img.shields.io/nuget/v/CodeFoxtrot.ParagonAtmLibrary?style=for-the-badge)](https://www.nuget.org/packages/CodeFoxtrot.ParagonAtmLibrary/)

## How to use
Add latest **CodeFoxtrot.ParagonAtmLibrary** to your project via 'Manage Nuget Packages'... 

or via command line...

```
dotnet add package CodeFoxtrot.ParagonAtmLibrary --version 1.4.9
```

or via your .csproj file...

```
<PackageReference Include="CodeFoxtrot.ParagonAtmLibrary" Version="1.4.9" />
```

Add this to your ConfigureServices() method:

```
services.AddParagonAtmLibrary();
```

### Set your preferences -- Simulation.json
Specify your WebFast account, API endpoints if different from the defaults, a download path for storing screenshots, and the connection details for the Virtual ATM:

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
    "AppStartup": "C:\\Users\\Public\\Desktop\\SSTAuto1.BAT",
    "StandardDelay": 5000
  }
}
```

### Define your screen pool -- AvailableScreens.json
This JSON is used to define each screen. This is completely subjective, so you'll need to adjust accordingly with your testing.  

Each screen is given a name, an array of phrases and a confidence level.  The array of phrases allows you to define multiple potential values, e.g. English vs Spanish phrases.  Or you might have different scenarios with different text.  

The **MatchConfidence** is provided to accomodate variability with the Paragon screen OCR technology. Not always will OCR read every word correctly, so how much of specific phrase do you require to match, in order to say the current ATM screen is a match for what you've defined here...  

```
"AvailableScreens": [
    {
      "Name": "Welcome",
      "Text": [
        "Please insert and remove your card",
        "Por favor inserte su tarjeta"
      ],
      "MatchConfidence": 0.80
    },
    {
      "Name": "OutOfService",
      "Text": [
        "This ATM is temporarily unavailable We're sorry for the inconvenience"
      ],
      "MatchConfidence": 0.80
    },
    {
      "Name": "LanguageSelection",
      "Text": [
        "Please select the language you wish to use",
        "Por favor seleccione el idioma que desea usar"
      ],
      "MatchConfidence": 0.80
    },
    {
      "Name": "PIN",
      "Text": [
        "Please enter your Personal Identification Number",
        "Por favor ingrese su Numero de Identificacion Personal"
      ],
      "MatchConfidence": 0.80
    },
    {
      "Name": "TransactionType",
      "Text": [
        "Please select the type of transaction by pressing the appropriate key",
        "Por favor escoja el tipo de transaccion oprimiendo la tecla apropiada"
      ],
      "MatchConfidence": 0.80
    },
    {
      "Name": "AccountType",
      "Text": [
        "Please select an account type",
        "Cuenta Corriente"
      ],
      "MatchConfidence": 1.00
    },
    {
      "Name": "BalanceDestination",
      "Text": [
        "Where would you like your balance",
        "Donde desea ver su saldo"
      ],
      "MatchConfidence": 0.80
    },
    {
      "Name": "FastCash",
      "Text": [
        "Please select a Fast Cash amount"
      ],
      "MatchConfidence": 0.80
    },
    {
      "Name": "PleaseWait",
      "Text": [
        "Please wait"
      ],
      "MatchConfidence": 1.00
    },
    {
      "Name": "AccountName",
      "Text": [
        "Please select the desired account",
        "Por favor escoja la cuenta deseada"
      ],
      "MatchConfidence": 0.80
    },
    {
      "Name": "BalanceInquiry",
      "Text": [
        "Balance inquiry Available Total",
        "Saldo para Disponible total"
      ],
      "MatchConfidence": 0.80
    },
    {
      "Name": "AnotherTransaction",
      "Text": [
        "Do you want another transaction",
        "Desea otra transaccion",
        "Desea otra transaccien"
      ],
      "MatchConfidence": 0.60
    },
    {
      "Name": "MoreTime",
      "Text": [
        "Do you need more time",
        "Would you like more time",
        "Quiere usted mas tiempo"
      ],
      "MatchConfidence": 0.80
    },
    {
      "Name": "TransactionCancelled",
      "Text": [
        "Your transaction has been cancelled"
      ],
      "MatchConfidence": 0.80
    },
    {
      "Name": "ThankYou",
      "Text": [
        "Thank you for using this ATM",
        "Gracias por usar este cajero de"
      ],
      "MatchConfidence": 0.80
    },
    {
      "Name": "TakeCard",
      "Text": [
        "Please take your card",
        "Por favor retire su tarjeta"
      ],
      "MatchConfidence": 1.00
    }
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
  | DisconnectAsync()      | GetScreenWordsAsync()              |
  | DispatchToIdleAsync()  | MatchScreen() / MatchScreenAsync() |
  | TakeAllMediaAsync()    | WaitForScreenAsync()               |
  | SaveScreenshotsync()   | WaitForTextAsync()                 |
  
</div>

### Sample Balance Inquiry on an NCR Edge Virtual ATM -- ConsumerTransactionService.cs
Note this is totally dependent on your ATMs screen flow!  

```
public async Task BalanceInquiry()
{
    try
    {
        string saveFolder = Path.Combine(_config["Preferences:DownloadPath"], DateTime.Now.ToString("yyyy-MM-dd--HH.mm.ss"));
        string cardId = "f2305283-bb84-49fe-aba6-cd3f7bcfa5ba";
        string cardPin = "1234";
        string language = "English";
        string transactionType = "Account Balance";
        string accountType = "Checking";
        string accountName = "Checking|T";
        string receiptOption = "Print and Display";

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

        // Get location of transaction language
        LocationModel location = await _vmService.GetLocationByTextAsync(language);

        if (location is null || location.Found == false)
        {
            _logger.LogError($"{language} not found");
            return;
        }

        // Click language button
        success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

        if (success == false)
        {
            _logger.LogError($"Failed to click {language}");
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

        // Find account balance button
        location = await _vmService.GetLocationByTextAsync(transactionType);

        if (location is null || location.Found == false)
        {
            _logger.LogError($"{transactionType} option not found");
            return;
        }

        // Click account balance button
        success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

        if (success == false)
        {
            _logger.LogError($"Failed to click {transactionType}");
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

        // Find transaction account type
        location = await _vmService.GetLocationByTextAsync(accountType);

        if (location is null || location.Found == false)
        {
            _logger.LogError($"{accountType} account option not found");
            return;
        }

        // Click account type
        success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

        if (success == false)
        {
            _logger.LogError($"Failed to click {accountType} account");
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

        // Find display balance button
        location = await _vmService.GetLocationByTextAsync(receiptOption);

        if (location is null || location.Found == false)
        {
            _logger.LogError($"{receiptOption} option not found");
            return;
        }

        // Click display balance button
        success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

        if (success == false)
        {
            _logger.LogError($"Failed to click {receiptOption}");
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

        // Find specified account button
        location = await _vmService.GetLocationByTextAsync(accountName);

        if (location is null || location.Found == false)
        {
            _logger.LogError($"{accountName} account not found");
            return;
        }

        // Click specified account button
        success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

        if (success == false)
        {
            _logger.LogError($"Failed to click {accountName}");
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
        ReceiptModel receipt = await _atmService.TakeReceiptAsync(receiptPrinter.Name);

        if (receipt is not null)
        {
            string receiptText = JsonSerializer.Serialize(receipt.OcrData.Elements.ToList().Select(e => e.text));
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

        // Find no button
        location = await _vmService.GetLocationByTextAsync("no");

        if (location is not null && location.Found)
        {
            // Click no button
            await _vmService.ClickScreenAsync(new ClickScreenModel(location));
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
