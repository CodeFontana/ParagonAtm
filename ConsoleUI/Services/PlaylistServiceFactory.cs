using ConsoleUI.Interfaces;
using ConsoleUI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Interfaces;

namespace ConsoleUI.Services;

public class PlaylistServiceFactory : IPlaylistServiceFactory
{
    public PlaylistService GetPlaylistService(IConfiguration config,
                                              ILoggerFactory loggerFactory,
                                              List<TransactionModel> availableTransactions,
                                              PlaylistModel playlistModel,
                                              IClientService clientService,
                                              IAtmService atmService,
                                              IVirtualMachineService vmService,
                                              IAutomationService autoService)
    {
        return new PlaylistService(config,
                                   loggerFactory,
                                   availableTransactions,
                                   playlistModel,
                                   clientService,
                                   atmService,
                                   vmService,
                                   autoService);
    }
}
