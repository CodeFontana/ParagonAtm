using ConsoleUI.Models;
using ConsoleUI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Interfaces;

namespace ConsoleUI.Interfaces;
public interface IPlaylistServiceFactory
{
    PlaylistService GetPlaylistService(IConfiguration config,
                                       ILoggerFactory loggerFactory,
                                       List<TransactionModel> availableTransactions,
                                       PlaylistModel playlistModel,
                                       IClientService clientService,
                                       IAtmService atmService,
                                       IAutomationService autoService);
}