using ConsoleUI.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Interfaces;

namespace VirtualAtmClient;

public class ClientApp : IHostedService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<ClientApp> _logger;
    private readonly IAgentService _agentService;
    private readonly IConnectionService _connectionService;
    private readonly IClientService _clientService;
    private readonly IConsumerTransactionService _consumerTransactionService;
    private readonly ITransactionService _transactionService;

    public ClientApp(IHostApplicationLifetime hostApplicationLifetime,
                     ILogger<ClientApp> logger,
                     IAgentService agentService,
                     IConnectionService connectionService,
                     IClientService clientService,
                     IConsumerTransactionService consumerTransactionService,
                     ITransactionService transactionService)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
        _agentService = agentService;
        _connectionService = connectionService;
        _clientService = clientService;
        _consumerTransactionService = consumerTransactionService;
        _transactionService = transactionService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _hostApplicationLifetime.ApplicationStarted.Register(async () =>
        {
            try
            {
                await Task.Yield(); // https://github.com/dotnet/runtime/issues/36063
                await Task.Delay(1000); // Additional delay for Microsoft.Hosting.Lifetime messages
                await ExecuteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unhandled exception!");
            }
            finally
            {
                _hostApplicationLifetime.StopApplication();
            }
        });

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _clientService.DisconnectAsync();
    }

    public async Task ExecuteAsync()
    {
        try
        {
            if (_transactionService.LoadUserData() == false)
            {
                return;
            }
            else if (await _clientService.ConnectAsync() == false)
            {
                return;
            }

            await _consumerTransactionService.BalanceInquiry();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error");
        }
    }        
}
