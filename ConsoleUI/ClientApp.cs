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
    private readonly IEdgeConsumerTransactionService _edgeConsumerTransactionService;

    public ClientApp(IHostApplicationLifetime hostApplicationLifetime,
                     ILogger<ClientApp> logger,
                     IClientService clientService,
                     IEdgeConsumerTransactionService edgeConsumerTransactionService)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
        _clientService = clientService;
        _edgeConsumerTransactionService = edgeConsumerTransactionService;
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

            await _edgeConsumerTransactionService.BalanceInquiry("f2305283-bb84-49fe-aba6-cd3f7bcfa5ba",
                                                             "1234",
                                                             "English",
                                                             "Checking",
                                                             "Checking|T",
                                                             "Print and Display");

            await _edgeConsumerTransactionService.BalanceInquiry("f2305283-bb84-49fe-aba6-cd3f7bcfa5ba",
                                                             "1234",
                                                             "Espanol",
                                                             "Cuenta Corriente",
                                                             "Checking|T",
                                                             "Imprimir y Mostrar");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unexpected error");
        }
    }        
}
