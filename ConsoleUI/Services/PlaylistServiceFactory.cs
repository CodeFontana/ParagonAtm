using ConsoleUI.Interfaces;
using ConsoleUI.Models;
using Microsoft.Extensions.Logging;

namespace ConsoleUI.Services;

public class PlaylistServiceFactory : IPlaylistServiceFactory
{
    public PlaylistService GetPlaylistService(ILoggerFactory loggerFactory, List<TransactionModel> availableTransactions, PlaylistModel playlistModel)
    {
        return new PlaylistService(loggerFactory, availableTransactions, playlistModel);
    }
}
