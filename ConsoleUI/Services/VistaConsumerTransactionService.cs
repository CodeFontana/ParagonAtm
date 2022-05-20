using ConsoleUI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Interfaces;
using ParagonAtmLibrary.Models;
using System.Text.Json;

namespace ConsoleUI.Services;

public class VistaConsumerTransactionService : IVistaConsumerTransactionService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EdgeConsumerTransactionService> _logger;
    private readonly IAtmService _atmService;
    private readonly IAutomationService _autoService;
    private readonly IClientService _clientService;
    private readonly List<AtmScreenModel> _atmScreens;

    public VistaConsumerTransactionService(IConfiguration configuration,
                                           ILogger<EdgeConsumerTransactionService> logger,
                                           IAtmService atmService,
                                           IAutomationService autoService,
                                           IClientService clientService)
    {
        _config = configuration;
        _logger = logger;
        _atmService = atmService;
        _autoService = autoService;
        _clientService = clientService;
        _atmScreens = _config.GetSection("AvailableScreens").Get<List<AtmScreenModel>>();
    }

    /// <summary>
    /// Sample Balance Inquiry transaction, with hard-coded responses
    /// and screen flow.
    /// </summary>
    /// <remarks>
    /// It relies upon an array of AtmScreenModels being defined in 
    /// AvailableScreens.json, so screen can be properly matched and waited.
    /// </remarks>
    /// <remarks>
    /// This approach is useful for C# developers as transactions can
    /// be built purely in C# code.
    /// </remarks>
    /// <returns>A task representing a balance inquiry transaction</returns>
    public async Task BalanceInquiry(string cardId = "f2305283-bb84-49fe-aba6-cd3f7bcfa5ba",
                                     string cardPin = "1234",
                                     string receiptOption = "Display Balance",
                                     string accountType = "Checking",
                                     string accountName = "Checking|T")
    {
        try
        {
            string saveFolder = Path.Combine(_config["Preferences:DownloadPath"], DateTime.Now.ToString("yyyy-MM-dd--HH.mm.ss"));

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

            // Validate -- PIN screen
            atScreen = await _autoService.WaitForScreenAsync(
                _atmScreens.First(s => s.Name.ToLower() == "pin"),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(5));

            if (atScreen == false)
            {
                _logger.LogError("PIN screen not found");
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

            // Validate -- Would you like your account balance?
            AtmScreenModel balanceRequest = _atmScreens.First(s => s.Name.ToLower() == "accountbalance");
            atScreen = await _autoService.WaitForScreenAsync(balanceRequest,
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(5));

            if (atScreen == false)
            {
                _logger.LogError($"ATM not at balance request screen");
                return;
            }

            await _clientService.SaveScreenshotAsync(saveFolder);

            if (await _autoService.FindAndClickAsync("Yes") == false)
            {
                _logger.LogError($"Failed to find and click 'Yes' button");
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

            if (await _autoService.FindAndClickAsync("print") == false)
            {
                _logger.LogError("Failed to find and click 'Print' button");
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
                _logger.LogInformation($"Take receipt -- {receiptText}");
            }

            // Validate -- Transaction complete
            atScreen = await _autoService.WaitForScreenAsync(
                _atmScreens.First(s => s.Name.ToLower() == "transactioncomplete"),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(5));

            if (atScreen == false)
            {
                _logger.LogError("Another transaction screen not found");
                return;
            }

            await _clientService.SaveScreenshotAsync(saveFolder);

            if (await _autoService.FindAndClickAsync("Return card"))
            {
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
            }
            else if (await _autoService.FindAndClickAsync("Return card"))
            {
                for (int i = 0; i < 4; i++)
                {
                    await _atmService.TakeCardAsync();
                    await Task.Delay(500);
                }
                
                await _clientService.SaveScreenshotAsync(saveFolder);
            }
            else
            {
                _logger.LogError($"Failed to find and click 'Exit' or 'Return card' button");
                return;
            }

            await Task.Delay(standardDelay);
            await _clientService.SaveScreenshotAsync(saveFolder);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Balance inquiry failed");
        }
    }
}
