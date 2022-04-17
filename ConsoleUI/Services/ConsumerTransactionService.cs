using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Models;
using ParagonAtmLibrary.Services;

namespace ConsoleUI.Services;

public class ConsumerTransactionService : IConsumerTransactionService
{
    private readonly IConfiguration _config;
    private readonly ILogger<ConsumerTransactionService> _logger;
    private readonly IVirtualMachineService _vmService;
    private readonly IAtmService _atmService;
    private readonly IAutomationService _autoService;
    private readonly IClientService _clientService;
    private readonly List<AtmScreenModel> _atmScreens;

    public ConsumerTransactionService(IConfiguration configuration,
                                      ILogger<ConsumerTransactionService> logger,
                                      IVirtualMachineService vmService,
                                      IAtmService atmService,
                                      IAutomationService autoService,
                                      IClientService clientService)
    {
        _config = configuration;
        _logger = logger;
        _vmService = vmService;
        _atmService = atmService;
        _autoService = autoService;
        _clientService = clientService;
        _atmScreens = _config.GetSection("ConsumerTransaction:ScreenDefinitions").Get<List<AtmScreenModel>>();
    }

    public async Task BalanceInquiry()
    {
        try
        {
            // Starting point -- InService/Welcome screen
            AtmScreenModel welcome = _atmScreens.First(s => s.Name.ToLower() == "welcome");
            bool atScreen = await _autoService.MatchScreenAsync(welcome);

            if (atScreen == false)
            {
                _logger.LogError("ATM not at welcome screen");
                return;
            }

            string saveFolder = Path.Combine(_config["Preferences:DownloadPath"], DateTime.Now.ToString("yyyy-MM-dd--HH.mm.ss"));
            await _clientService.SaveScreenShot(saveFolder);

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
            CardModel card = new(_config["ConsumerTransaction:Card:Id"], cardReader.Name);
            bool success = await _atmService.InsertCardAsync(card);

            if (success == false)
            {
                _logger.LogError("Failed to insert card for transaction");
                return;
            }

            int standardDelay = _config.GetValue<int>("ConsumerTransaction:StandardDelay");
            await Task.Delay(standardDelay);

            // Validate -- Language selection screen
            atScreen = await _autoService.WaitForScreenAsync(
                _atmScreens.First(s => s.Name.ToLower() == "languageselection"),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(5));

            if (atScreen == false)
            {
                _logger.LogError($"ATM not at language screen");
                return;
            }

            await _clientService.SaveScreenShot(saveFolder);

            // Get location of transaction language
            string language = _config["ConsumerTransaction:Language"];
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

            await _clientService.SaveScreenShot(saveFolder);

            // Isolate pinpad service
            AtmServiceModel pinpad = services?.FirstOrDefault(x => x.DeviceType.ToLower() == "pin");

            if (pinpad == null)
            {
                _logger.LogError($"Pinpad not found in device list");
                return;
            }

            // Type PIN
            foreach (char c in _config["ConsumerTransaction:Card:Pin"])
            {
                success = await _atmService.PressKeyAsync(new PressKeyModel(pinpad.Name, $"n{c}"));

                if (success == false)
                {
                    _logger.LogError($"Failed to press pinpad key -- {c}");
                    return;
                }

                await Task.Delay(1000);
            }

            await _clientService.SaveScreenShot(saveFolder);

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

            await _clientService.SaveScreenShot(saveFolder);

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

            await _clientService.SaveScreenShot(saveFolder);

            // Find transaction account type
            string accountType = _config["ConsumerTransaction:AccountType"];
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
                _logger.LogError("Failed to click checking account");
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

            await _clientService.SaveScreenShot(saveFolder);

            // Find display balance button
            string receiptOption = _config["ConsumerTransaction:ReceiptOption"];
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

            await _clientService.SaveScreenShot(saveFolder);

            // Find specified account button
            string accountName = _config["ConsumerTransaction:AccountName"];
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

            await _clientService.SaveScreenShot(saveFolder);

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
                _logger.LogInformation("Receipt text...");
                receipt.OcrData.Elements.ToList()
                    .ForEach(e => _logger.LogInformation(e.text));
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

            await _clientService.SaveScreenShot(saveFolder);

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

            await _clientService.SaveScreenShot(saveFolder);
            await _atmService.TakeCardAsync();
            await Task.Delay(standardDelay);
            await _clientService.SaveScreenShot(saveFolder);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Balance inquiry failed");
        }
    }
}
