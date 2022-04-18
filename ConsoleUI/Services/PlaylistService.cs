using ConsoleUI.Interfaces;
using ConsoleUI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Interfaces;
using ParagonAtmLibrary.Models;

namespace ConsoleUI.Services;

public class PlaylistService : IPlaylistService
{
    private readonly ILogger _logger;
    private readonly IConfiguration _config;
    private readonly List<TransactionModel> _availableTransactions;
    private readonly PlaylistModel _playlistModel;
    private readonly IClientService _clientService;
    private readonly IAtmService _atmService;
    private readonly IVirtualMachineService _vmService;
    private readonly IAutomationService _autoService;
    private readonly List<AtmScreenModel> _atmScreens;

    public PlaylistService(IConfiguration config,
                           ILoggerFactory loggerFactory,
                           List<TransactionModel> availableTransactions,
                           PlaylistModel playlistModel,
                           IClientService clientService,
                           IAtmService atmService,
                           IVirtualMachineService virtualMachine,
                           IAutomationService autoService)
    {
        _logger = loggerFactory.CreateLogger(playlistModel.Name);
        _config = config;
        _availableTransactions = availableTransactions;
        _playlistModel = playlistModel;
        _clientService = clientService;
        _atmService = atmService;
        _vmService = virtualMachine;
        _autoService = autoService;
        _atmScreens = _config.GetSection("AvailableScreens").Get<List<AtmScreenModel>>();
    }

    public async Task<bool> RunPlaylist()
    {
        if (ValidatePlayList() == false)
        {
            return false;
        }

        bool playlistSuccess = true;

        for (int i = 0; i < _playlistModel.Options.Repeat; i++)
        {
            foreach (string playlistTransaction in _playlistModel.Transactions)
            {
                TransactionModel t = _availableTransactions.First(at => at.Name.ToLower() == playlistTransaction.ToLower());
                playlistSuccess &= await RunTransaction(t);

                if (playlistSuccess == false)
                {
                    return false;
                }

                if (await _clientService.DispatchToIdle() == false)
                {
                    return false;
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(_playlistModel.Options.RepeatSeconds));
        }

        return true;
    }

    private async Task<bool> RunTransaction(TransactionModel transaction)
    {
        try
        {
            string saveFolder = Path.Combine(_config["Preferences:DownloadPath"], DateTime.Now.ToString("yyyy-MM-dd--HH.mm.ss"));
            List<AtmServiceModel> services = await _atmService.GetServicesAsync();

            if (services is null)
            {
                _logger.LogError($"ATM service list is empty");
                return false;
            }

            foreach (TransactionScreenFlowModel sfm in transaction.ScreenFlow)
            {
                await _clientService.SaveScreenShot(saveFolder);
                _logger.LogInformation($"Processing screen -- {sfm.Screen}");
                AtmScreenModel requestedScreen = _atmScreens.FirstOrDefault(s => s.Name.ToLower() == sfm.Screen.ToLower());

                if (requestedScreen is null)
                {
                    _logger.LogError("Requested screen is not available");
                    _logger.LogError($"Available screens -- {string.Join(", ", _atmScreens.Select(s => s.Name))}");
                    return false;
                }

                bool foundScreen = await _autoService.WaitForScreenAsync(requestedScreen, TimeSpan.FromSeconds(sfm.TimeoutSeconds), TimeSpan.FromSeconds(sfm.RefreshSeconds));

                if (foundScreen == false)
                {
                    _logger.LogError($"ATM not displaying expected screen -- {requestedScreen.Name}");
                    return false;
                }

                switch (sfm.ActionType.ToLower())
                {
                    case "insertcard":
                        AtmServiceModel cardReader = services.FirstOrDefault(x => x.DeviceType.ToLower() == "idc");

                        if (cardReader == null)
                        {
                            _logger.LogError($"Card reader not found in device list");
                            return false;
                        }
                        else if (cardReader.IsOpen == false)
                        {
                            _logger.LogError($"Card reader is not open");
                            return false;
                        }

                        bool success = await _atmService.InsertCardAsync(new CardModel(sfm.ActionValue, cardReader.Name));

                        if (success == false)
                        {
                            _logger.LogError("Failed to insert card for transaction");
                            return false;
                        }

                        break;

                    case "button":
                        LocationModel location = await _vmService.GetLocationByTextAsync(sfm.ActionValue);

                        if (location is null || location.Found == false)
                        {
                            _logger.LogError($"{sfm.ActionValue} not found");
                            return false;
                        }

                        success = await _vmService.ClickScreenAsync(new ClickScreenModel(location));

                        if (success == false)
                        {
                            _logger.LogError($"Failed to click {sfm.ActionValue}");
                            return false;
                        }

                        break;

                    case "keypad":
                        AtmServiceModel pinpad = services.FirstOrDefault(x => x.DeviceType.ToLower() == "pin");

                        if (pinpad == null)
                        {
                            _logger.LogError($"Pinpad not found in device list");
                            return false;
                        }

                        foreach (char c in sfm.ActionValue)
                        {
                            success = await _atmService.PressKeyAsync(new PressKeyModel(pinpad.Name, $"n{c}"));

                            if (success == false)
                            {
                                _logger.LogError($"Failed to press pinpad key -- {c}");
                                return false;
                            }

                            await Task.Delay(TimeSpan.FromSeconds(transaction.Options.KeypadDelaySeconds));
                        }

                        success = await _atmService.PressKeyAsync(new PressKeyModel(pinpad.Name, "Enter"));

                        if (success == false)
                        {
                            _logger.LogError("Failed to press pinpad key -- Enter");
                            return false;
                        }

                        break;

                    case "takecard":
                        await _clientService.TakeAllMedia();
                        break;

                    default:
                        _logger.LogError($"Unknown action type -- {sfm.ActionType}");
                        return false;
                }

                await Task.Delay(TimeSpan.FromSeconds(transaction.Options.StandardDelaySeconds));
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Transaction failed -- {transaction.Name}");
            return false;
        }
    }

    private bool ValidatePlayList()
    {
        _logger.LogInformation($"Validating playlist -- {_playlistModel.Name}");
        bool isValid = true;

        _playlistModel.Transactions.ForEach(t =>
        {
            if (_availableTransactions.Any(at => at.Name.ToLower() == t.ToLower()) == false)
            {
                _logger.LogError($"Transaction {t} is not defined in the list of available transactions.");
                isValid = false;
            }
        });

        if (isValid == false)
        {
            _logger.LogError("Playlist validation failed");
            _logger.LogError($"Available transactions -- {string.Join(", ", _availableTransactions.Select(t => t.Name))}");
            return false;
        }

        _logger.LogInformation($"Successfully validated -- {_playlistModel.Name}");

        return true;
    }
}
