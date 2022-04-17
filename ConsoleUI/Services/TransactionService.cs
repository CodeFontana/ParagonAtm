using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Models;
using ParagonAtmLibrary.Services;

namespace ConsoleUI.Services;

public class TransactionService : ITransactionService
{
    private readonly IConfiguration _config;
    private readonly ILogger<TransactionService> _logger;
    private readonly IVirtualMachineService _vmService;
    private readonly IAtmService _atmService;
    private readonly IAutomationService _autoService;
    private readonly IClientService _clientService;

    public TransactionService(IConfiguration configuration,
                              ILogger<TransactionService> logger,
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
    }

    public async Task RunTransactions()
    {
        await Task.Delay(1000);
    }
}
