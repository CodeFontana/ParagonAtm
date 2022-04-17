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
    private readonly IAutomationService _autoService;
    private readonly List<AtmScreenModel> _atmScreens;

    public PlaylistService(IConfiguration config,
                           ILoggerFactory loggerFactory,
                           List<TransactionModel> availableTransactions,
                           PlaylistModel playlistModel,
                           IClientService clientService,
                           IAtmService atmService,
                           IAutomationService autoService)
    {
        _logger = loggerFactory.CreateLogger(playlistModel.Name);
        _config = config;
        _availableTransactions = availableTransactions;
        _playlistModel = playlistModel;
        _clientService = clientService;
        _atmService = atmService;
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

        foreach (string playlistTransaction in _playlistModel.Transactions)
        {
            TransactionModel t = _availableTransactions.First(at => at.Name.ToLower() == playlistTransaction.ToLower());
            playlistSuccess &= await RunTransaction(t);

            if (playlistSuccess == false)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> RunTransaction(TransactionModel transaction)
    {
        try
        {
            string saveFolder = Path.Combine(_config["Preferences:DownloadPath"], DateTime.Now.ToString("yyyy-MM-dd--HH.mm.ss"));
            await _clientService.SaveScreenShot(saveFolder);

            foreach (TransactionScreenFlowModel sfm in transaction.ScreenFlow)
            {
                _logger.LogInformation($"Processing screen -- {sfm.Screen}");
                AtmScreenModel requestedScreen = _atmScreens.FirstOrDefault(s => s.Name.ToLower() == sfm.Screen.ToLower());

                if (requestedScreen is null)
                {
                    _logger.LogError("Requested screen is not available");
                    _logger.LogError($"Available screens -- {string.Join(", ", _atmScreens.Select(s => s.Name))}");
                    return false;
                }

                bool foundScreen = await _autoService.WaitForScreenAsync(requestedScreen, TimeSpan.FromSeconds(sfm.Timeout), TimeSpan.FromSeconds(sfm.RefreshInterval));

                if (foundScreen == false)
                {
                    _logger.LogError($"ATM not displaying expected screen -- {requestedScreen.Name}");
                    return false;
                }

                switch (sfm.ActionType.ToLower())
                {
                    case "insertcard":
                        List<AtmServiceModel> services = await _atmService.GetServicesAsync();

                        if (services is null)
                        {
                            _logger.LogError($"ATM service list is empty");
                            return false;
                        }

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

                        await Task.Delay(transaction.Options.StandardDelay);
                        break;

                    default:
                        _logger.LogError($"Unknown action type -- {sfm.ActionType}");
                        return false;
                }

                await _clientService.SaveScreenShot(saveFolder);
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
