using ConsoleUI.Interfaces;
using ConsoleUI.Models;
using Microsoft.Extensions.Logging;

namespace ConsoleUI.Services;

public class PlaylistService : IPlaylistService
{
    private readonly ILogger _logger;
    private readonly List<TransactionModel> _availableTransactions;
    private readonly PlaylistModel _playlistModel;

    public PlaylistService(ILoggerFactory loggerFactory,
                           List<TransactionModel> availableTransactions,
                           PlaylistModel playlistModel)
    {
        _logger = loggerFactory.CreateLogger(playlistModel.Name);
        _availableTransactions = availableTransactions;
        _playlistModel = playlistModel;
    }

    public bool RunPlaylist()
    {
        if (ValidatePlayList() == false)
        {
            return false;
        }

        bool playlistSuccess = true;

        _playlistModel.Transactions.ForEach(pt =>
        {
            TransactionModel t = _availableTransactions.First(at => at.Name.ToLower() == pt.ToLower());
            playlistSuccess &= RunTransaction(t);
        });

        return playlistSuccess;
    }

    private bool RunTransaction(TransactionModel transaction)
    {
        try
        {
            transaction.ScreenFlow.ForEach(s =>
            {

            });

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
            _logger.LogInformation($"Available transactions -- {string.Join(", ", _availableTransactions.Select(t => t.Name))}");
            _logger.LogError("Playlist validation failed");
            return false;
        }

        _logger.LogInformation($"Successfully validated -- {_playlistModel.Name}");

        return true;
    }
}
