namespace ConsoleUI.Services;

public class ActivateEnterpriseBalanceInquiryService : IActivateEnterpriseBalanceInquiryService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EdgeBalanceInquiryService> _logger;
    private readonly IAtmService _atmService;
    private readonly IAutomationService _autoService;
    private readonly IClientService _clientService;
    private readonly List<AtmScreenModel> _atmScreens;
    private readonly string _simulationProfile;

    public ActivateEnterpriseBalanceInquiryService(IConfiguration configuration,
                                                   ILogger<EdgeBalanceInquiryService> logger,
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
        _simulationProfile = _config[$"Preferences:SimulationProfile"];
    }

    private async Task TakeReceipt(List<AtmServiceModel> services, string saveFolder)
    {
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
    public async Task BalanceInquiry(CancellationToken cancelToken,
                                     string cardId,
                                     string cardPin,
                                     string language,
                                     string accountType,
                                     string accountName)
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
            if (cancelToken.IsCancellationRequested) { _logger.LogInformation("Transaction cancelled"); return; }

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

            int standardDelay = _config.GetValue($"Terminal.{_simulationProfile}:StandardDelayMS", 2000);
            await Task.Delay(standardDelay);

            // Validate -- Language selection screen
            AtmScreenModel languageScreen = _atmScreens.First(s => s.Name.ToLower() == "languageselection");
            atScreen = await _autoService.WaitForScreenAsync(languageScreen,
                TimeSpan.FromSeconds(20),
                TimeSpan.FromMilliseconds(standardDelay));

            if (atScreen == false)
            {
                _logger.LogError($"ATM not at language screen");
                return;
            }

            await _clientService.SaveScreenshotAsync(saveFolder);
            if (cancelToken.IsCancellationRequested) { _logger.LogInformation("Transaction cancelled"); return; }

            if (await _autoService.FindAndClickAsync(language) == false)
            {
                _logger.LogError($"Failed to find and click '{language}' button");
                return;
            }

            await Task.Delay(standardDelay);

            // Validate -- PIN screen
            atScreen = await _autoService.WaitForScreenAsync(_atmScreens.First(s => s.Name.ToLower() == "pin"),
                TimeSpan.FromSeconds(20),
                TimeSpan.FromMilliseconds(standardDelay));

            if (atScreen == false)
            {
                _logger.LogError("Enter your PIN screen not found");
                return;
            }

            await _clientService.SaveScreenshotAsync(saveFolder);
            if (cancelToken.IsCancellationRequested) { _logger.LogInformation("Transaction cancelled"); return; }

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
            if (cancelToken.IsCancellationRequested) { _logger.LogInformation("Transaction cancelled"); return; }

            // Press ENTER
            success = await _atmService.PressKeyAsync(new PressKeyModel(pinpad.Name, "Enter"));

            if (success == false)
            {
                _logger.LogError("Failed to press pinpad key -- Enter");
                return;
            }

            await Task.Delay(standardDelay);

            // Validate -- Transaction type screen
            atScreen = await _autoService.WaitForScreenAsync(_atmScreens.First(s => s.Name.ToLower() == "transactiontype"),
                TimeSpan.FromSeconds(20),
                TimeSpan.FromMilliseconds(standardDelay));

            if (atScreen == false)
            {
                _logger.LogError("Transaction type screen not found");
                return;
            }

            await _clientService.SaveScreenshotAsync(saveFolder);
            if (cancelToken.IsCancellationRequested) { _logger.LogInformation("Transaction cancelled"); return; }

            if (await _autoService.FindAndClickAsync(transactionType) == false)
            {
                _logger.LogError($"Failed to find and click '{transactionType}' button");
                return;
            }

            await Task.Delay(standardDelay);

            // Validate -- Account type screen
            atScreen = await _autoService.WaitForScreenAsync(_atmScreens.First(s => s.Name.ToLower() == "accounttype"),
                TimeSpan.FromSeconds(20),
                TimeSpan.FromMilliseconds(standardDelay));

            if (atScreen == false)
            {
                _logger.LogError("Account type screen not found");
                return;
            }

            await _clientService.SaveScreenshotAsync(saveFolder);
            if (cancelToken.IsCancellationRequested) { _logger.LogInformation("Transaction cancelled"); return; }

            if (await _autoService.FindAndClickAsync(accountType) == false)
            {
                _logger.LogError($"Failed to find and click '{accountType}' button");
                return;
            }

            await Task.Delay(standardDelay);

            // Validate -- Account type screen (again)
            atScreen = await _autoService.WaitForScreenAsync(_atmScreens.First(s => s.Name.ToLower() == "accounttype"),
                TimeSpan.FromSeconds(20),
                TimeSpan.FromMilliseconds(standardDelay));

            if (atScreen == false)
            {
                _logger.LogError("Account name screen not found");
                return;
            }

            await _clientService.SaveScreenshotAsync(saveFolder);
            if (cancelToken.IsCancellationRequested) { _logger.LogInformation("Transaction cancelled"); return; }

            if (await _autoService.FindAndClickAsync(accountName) == false)
            {
                _logger.LogError($"Failed to find and click '{accountName}' button");
                return;
            }

            await Task.Delay(standardDelay);

            // Validate -- Another transaction screen
            atScreen = await _autoService.WaitForScreenAsync(_atmScreens.First(s => s.Name.ToLower() == "anothertransaction"),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromMilliseconds(standardDelay));

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
            atScreen = await _autoService.WaitForScreenAsync(_atmScreens.First(s => s.Name.ToLower() == "takecard"),
                TimeSpan.FromSeconds(20),
                TimeSpan.FromMilliseconds(standardDelay));

            if (atScreen == false)
            {
                _logger.LogError("Take card screen not found");
                return;
            }

            await _clientService.SaveScreenshotAsync(saveFolder);
            await _atmService.TakeCardAsync();
            await TakeReceipt(services, saveFolder);
            await Task.Delay(standardDelay);
            await _clientService.SaveScreenshotAsync(saveFolder);
            await _clientService.DispatchToIdleAsync();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, $"Balance inquiry failed -- {ex.Message}");
        }
    }
}
