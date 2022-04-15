using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Models;
using System.Text.Json;

namespace ParagonAtmLibrary.Services;

public class AutomationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AgentService> _logger;
    private readonly AtmService _atmService;
    private readonly VirtualMachineService _vmService;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly char[] _splitChars;

    public AutomationService(IConfiguration config,
                             ILogger<AgentService> logger,
                             AtmService atmService,
                             VirtualMachineService vmService)
    {
        _config = config;
        _logger = logger;
        _atmService = atmService;
        _vmService = vmService;
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        _splitChars = new[] { ' ', ',', '.', '?', ';', '\'', '\"', '(', ')', '[', ']', '\\', '/' };
    }

    public async Task<bool> CompareText(List<string> comparePhrases, decimal matchConfidence)
    {
        List<string> screenText = await GetScreenWords();
        return CompareText(screenText, comparePhrases, matchConfidence);
    }

    public bool CompareText(List<string> screenText, List<string> comparePhrases, decimal matchConfidence)
    {
        ArgumentNullException.ThrowIfNull(screenText);
        ArgumentNullException.ThrowIfNull(comparePhrases);
        ArgumentNullException.ThrowIfNull(matchConfidence);

        foreach (string phrase in comparePhrases)
        {
            int matchCount = 0;

            IEnumerable<string> matches = screenText
                .Select(s => s.ToLower())
                .Intersect(phrase.Split(_splitChars)
                    .Select(s => s.Trim().ToLower())
                    .Where(s => string.IsNullOrWhiteSpace(s) == false));

            if (matches.Count() > 0)
            {
                _logger.LogTrace($"Found Match -- {JsonSerializer.Serialize(matches)}");
                matchCount += matches.Count();
                decimal confidence = matchCount / (decimal)phrase.Split(_splitChars).Length;

                if (confidence >= matchConfidence)
                {
                    _logger.LogTrace($"Matched -- {JsonSerializer.Serialize(phrase)} -- Confidence {confidence}");
                    return true;
                }

                _logger.LogTrace($"NotMatched -- {JsonSerializer.Serialize(phrase)} -- Confidence {confidence}");
            }
        }

        return false;
    }

    public async Task<List<string>> GetScreenWords()
    {
        OcrDataModel screenText = await _vmService.GetScreenTextAsync();

        if (screenText == null)
        {
            _logger.LogError("Unable to read screen text");
            return null;
        }
        else
        {
            List<string> result = new();

            screenText.Elements.ToList()
                .ForEach(e => result.AddRange(
                    e.text.Split(_splitChars)
                        .Select(s => s.Trim().ToLower())
                        .Where(s => string.IsNullOrWhiteSpace(s) == false)));

            _logger.LogTrace($"Screen words -- {JsonSerializer.Serialize(result)}");

            return result;
        }
    }

    public async Task<bool> IsAtScreen(AtmScreenModel screen)
    {
        List<string> screenText = await GetScreenWords();
        return CompareText(screenText, screen.Text, screen.MatchConfidence);
    }

    public async Task<bool> IsAtScreen(List<AtmScreenModel> screens)
    {
        List<string> screenText = await GetScreenWords();

        foreach (AtmScreenModel s in screens)
        {
            if (CompareText(screenText, s.Text, s.MatchConfidence))
            {
                _logger.LogTrace($"Found match -- {s.Name}");
                return true;
            }
        }

        return false;
    }

    public async Task<bool> SaveScreenShot()
    {
        try
        {
            string jpeg = await _vmService.GetScreenJpegAsync();

            if (string.IsNullOrWhiteSpace(jpeg) == false)
            {
                Directory.CreateDirectory(_config["Preferences:DownloadPath"]);
                File.WriteAllBytes(
                    $@"{_config["Preferences:DownloadPath"]}\Screenshot-{DateTime.Now.ToString("yyyy-MM-dd--HH.mm.ss")}.jpg",
                    Convert.FromBase64String(jpeg));
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public async Task<bool> WaitForScreen(AtmScreenModel screen, TimeSpan timeout, TimeSpan refreshInterval)
    {
        _logger.LogTrace($"Wait for screen -- {screen.Name}");
        return await WaitForText(screen.Text, screen.MatchConfidence, timeout, refreshInterval);
    }

    public async Task<bool> WaitForText(string[] words, decimal matchConfidence, TimeSpan timeout, TimeSpan refreshInterval)
    {
        return await WaitForText(words.ToList(), matchConfidence, timeout, refreshInterval);
    }

    public async Task<bool> WaitForText(List<string> phrases, decimal matchConfidence, TimeSpan timeout, TimeSpan refreshInterval)
    {
        ArgumentNullException.ThrowIfNull(phrases);
        ArgumentNullException.ThrowIfNull(matchConfidence);
        ArgumentNullException.ThrowIfNull(timeout);
        ArgumentNullException.ThrowIfNull(refreshInterval);

        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;

            while (DateTime.Now < endTime)
            {
                if (await CompareText(phrases, matchConfidence))
                {
                    return true;
                }

                await Task.Delay(refreshInterval);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }
}
