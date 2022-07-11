using ConsoleUI.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Interfaces;

namespace VirtualAtmClient;

public class ClientApp : IHostedService
{
    private readonly IConfiguration _config;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<ClientApp> _logger;
    private readonly IClientService _clientService;
    private readonly IEdgeConsumerTransactionService _edgeConsumerTransactionService;
    private readonly IVistaConsumerTransactionService _vistaConsumerTransactionService;
    private readonly CancellationTokenSource _cancelTokenSource;
    private readonly string _simulationProfile;

    public ClientApp(IConfiguration configuration,
                     IHostApplicationLifetime hostApplicationLifetime,
                     ILogger<ClientApp> logger,
                     IClientService clientService,
                     IEdgeConsumerTransactionService edgeConsumerTransactionService,
                     IVistaConsumerTransactionService vistaConsumerTransactionService)
    {
        _config = configuration;
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
        _clientService = clientService;
        _edgeConsumerTransactionService = edgeConsumerTransactionService;
        _vistaConsumerTransactionService = vistaConsumerTransactionService;
        _cancelTokenSource = new CancellationTokenSource();
        _simulationProfile = _config[$"Preferences:SimulationProfile"];
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
                _logger.LogCritical(ex, ex.Message);
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
        _cancelTokenSource.Cancel();
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

            if (await _clientService.StartAtmFromDesktopAsync() == false)
            {
                return;
            }

            if (_simulationProfile.Equals("Edge", StringComparison.CurrentCultureIgnoreCase))
            {
                await _edgeConsumerTransactionService.BalanceInquiry(_cancelTokenSource.Token,
                                                                 "f2305283-bb84-49fe-aba6-cd3f7bcfa5ba",
                                                                 "1234",
                                                                 "English",
                                                                 "Checking",
                                                                 "Checking|T",
                                                                 "Print and Display");

                await _edgeConsumerTransactionService.BalanceInquiry(_cancelTokenSource.Token,
                                                                     "f2305283-bb84-49fe-aba6-cd3f7bcfa5ba",
                                                                     "1234",
                                                                     "Espanol",
                                                                     "Cuenta Corriente",
                                                                     "Checking|T",
                                                                     "Imprimir y Mostrar");
            }
            else if (_simulationProfile.Equals("Vista", StringComparison.CurrentCultureIgnoreCase))
            {
                await _vistaConsumerTransactionService.BalanceInquiry(_cancelTokenSource.Token,
                                                                  "f2305283-bb84-49fe-aba6-cd3f7bcfa5ba",
                                                                  "1234",
                                                                  "Display Balance",
                                                                  "Checking",
                                                                  "Checking|T");
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, ex.Message);
        }
    }        
}
