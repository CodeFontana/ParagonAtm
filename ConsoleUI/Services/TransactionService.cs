using ConsoleUI.Interfaces;
using ConsoleUI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Interfaces;
using System.Text.Json;

namespace ConsoleUI.Services;

public class TransactionService : ITransactionService
{
    private readonly IConfiguration _config;
    private readonly ILogger<TransactionService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IPlaylistServiceFactory _playlistServiceFactory;
    private readonly IClientService _clientService;
    private readonly IAtmService _atmService;
    private readonly IAutomationService _autoService;
    private readonly List<TransactionModel> _transactions;
    private readonly List<PlaylistModel> _playlists;

    public TransactionService(IConfiguration configuration,
                              ILogger<TransactionService> logger,
                              ILoggerFactory loggerFactory,
                              IPlaylistServiceFactory playlistServiceFactory,
                              IClientService clientService,
                              IAtmService atmService,
                              IAutomationService autoService)
    {
        _config = configuration;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _playlistServiceFactory = playlistServiceFactory;
        _clientService = clientService;
        _atmService = atmService;
        _autoService = autoService;
        _transactions = new List<TransactionModel>();
        _playlists = new List<PlaylistModel>();
    }

    public async Task RunPlaylists()
    {
        foreach (PlaylistModel p in _playlists)
        {
            IPlaylistService ps = _playlistServiceFactory.GetPlaylistService(_config, _loggerFactory, _transactions, p, _clientService, _atmService, _autoService);
            await ps.RunPlaylist();
        }
            }

    public bool LoadUserData()
    {
        _logger.LogInformation("Load user transactions...");
        bool result = LoadFromJson(_config["Preferences:TransactionsPath"], _transactions);
        _logger.LogInformation("Load user playlists...");
        result &= LoadFromJson(_config["Preferences:PlaylistsPath"], _playlists);
        return result;
    }

    public bool LoadFromJson<T>(string folderPath, List<T> itemList)
    {
        try
        {
            if (Directory.Exists(folderPath) == false)
            {
                string relativeFolderPath = Path.Combine(Environment.CurrentDirectory, folderPath);

                if (Directory.Exists(relativeFolderPath) == false)
                {
                    throw new DirectoryNotFoundException(folderPath);
                }

                folderPath = relativeFolderPath;
            }

            string[] files = Directory.GetFiles(folderPath, "*.json");

            if (files.Length == 0)
            {
                throw new FileNotFoundException($"No files found at {folderPath}");
            }

            files.ToList().ForEach(f =>
            {
                try
                {
                    string json = File.ReadAllText(f);
                    var t = JsonSerializer.Deserialize<T>(json);
                    itemList.Add(t);
                    _logger.LogInformation($"Loaded -- {t}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to load file -- {f}");
                }
            });

            if (itemList.Count == 0)
            {
                throw new Exception($"Failed to load from {JsonSerializer.Serialize(files)}");
            }

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to load required data");
            return false;
        }
    }
}
