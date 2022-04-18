using ConsoleUI.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Interfaces;

namespace VirtualAtmClient;

public class ClientApp : IHostedService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<ClientApp> _logger;
    private readonly IClientService _clientService;
    private readonly IConsumerTransactionService _consumerTransactionService;

    public ClientApp(IHostApplicationLifetime hostApplicationLifetime,
                     ILogger<ClientApp> logger,
                     IClientService clientService,
                     IConsumerTransactionService consumerTransactionService)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
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
            if (await _clientService.ConnectAsync() == false)
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
