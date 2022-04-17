using ConsoleUI.Models;
using ConsoleUI.Services;
using Microsoft.Extensions.Logging;

namespace ConsoleUI.Interfaces;
public interface IPlaylistServiceFactory
{
    PlaylistService GetPlaylistService(ILoggerFactory loggerFactory, List<TransactionModel> availableTransactions, PlaylistModel playlistModel);
}