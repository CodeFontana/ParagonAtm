using ConsoleUI.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Services;

namespace VirtualAtmClient;

public class ClientApp : IHostedService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<ClientApp> _logger;
    private readonly IAgentService _agentService;
    private readonly IConnectionService _connectionService;
    private readonly IClientService _clientService;
    private readonly IConsumerTransactionService _consumerTransactionService;

    public ClientApp(IHostApplicationLifetime hostApplicationLifetime,
                     ILogger<ClientApp> logger,
                     IAgentService agentService,
                     IConnectionService connectionService,
                     IClientService clientService,
                     IConsumerTransactionService consumerTransactionService)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
        _agentService = agentService;
        _connectionService = connectionService;
        _clientService = clientService;
        _consumerTransactionService = consumerTransactionService;
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
                _logger.LogError(ex, "Unhandled exception!");
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
        await _clientService.DispatchToIdle();
        await _connectionService.CloseAsync();
        await _agentService.CloseSesisonAsync();
    }

    public async Task ExecuteAsync()
    {
        try
        {
            bool sessionOpened = await _clientService.ConnectAsync();

            if (sessionOpened == false)
            {
                return;
            }

            await _consumerTransactionService.BalanceInquiry();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, ex.Message);
        }
    }        
}
