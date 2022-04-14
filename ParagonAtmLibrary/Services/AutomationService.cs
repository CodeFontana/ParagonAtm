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
            HashSet<string> result = new();

            screenText.Elements.ToList()
                .ForEach(e => e.lines.ToList()
                    .ForEach(l => l.words.ToList()
                        .ForEach(p => p.text.Split(" ").ToList()
                            .ForEach(w => result.Add(w)))));

            _logger.LogTrace($"Found words -- {JsonSerializer.Serialize(result)}");

            return result.ToList();
        }
    }

    public async Task<bool> IsAtScreen(AtmScreenModel screen)
    {
        List<string> screenText = await GetScreenWords();
        return MatchScreen(screenText, screen.Text, screen.MatchConfidence);
    }

    public async Task<bool> IsAtScreen(List<AtmScreenModel> screens)
    {
        List<string> screenText = await GetScreenWords();

        foreach (AtmScreenModel s in screens)
        {
            if (MatchScreen(screenText, s.Text, s.MatchConfidence))
            {
                _logger.LogTrace($"Found match -- {s.Name}");
                return true;
            }
        }

        return false;
    }

    public async Task<bool> MatchScreen(List<string> comparePhrases, decimal matchConfidence)
    {
        List<string> screenText = await GetScreenWords();
        return MatchScreen(screenText, comparePhrases, matchConfidence);
    }

    public bool MatchScreen(List<string> screenPhrases, List<string> comparePhrases, decimal matchConfidence)
    {
        ArgumentNullException.ThrowIfNull(screenPhrases);
        ArgumentNullException.ThrowIfNull(comparePhrases);
        ArgumentNullException.ThrowIfNull(matchConfidence);
        int matchCount = 0;

        foreach (string x in screenPhrases)
        {
            foreach (string y in comparePhrases)
            {
                List<string> left = x.Split(" ").ToList().Select(w => w.ToLower()).ToList();
                List<string> right = y.Split(" ").ToList().Select(w => w.ToLower()).ToList();

                IEnumerable<string> matches = left.Intersect(right);

                if (matches.Count() > 0)
                {
                    _logger.LogTrace($"Found Match -- {JsonSerializer.Serialize(matches)}");
                    matchCount += matches.Count();
                }
            }
        }

        int wordCount = 0;

        foreach (string phrase in comparePhrases)
        {
            wordCount += phrase.Split(" ").Count();
        }

        decimal confidence = matchCount / (decimal)wordCount;

        if (confidence >= matchConfidence)
        {
            _logger.LogTrace($"Matched -- {JsonSerializer.Serialize(comparePhrases)} -- Confidence {confidence}");
            return true;
        }

        _logger.LogTrace($"NotMatched -- {JsonSerializer.Serialize(comparePhrases)} -- Confidence {confidence}");

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

    public async Task<bool> SearchForText(string[] phrases, decimal matchConfidence)
    {
        ArgumentNullException.ThrowIfNull(phrases);
        return await SearchForText(phrases.ToList(), matchConfidence);
    }

    public async Task<bool> SearchForText(List<string> phrases, decimal matchConfidence)
    {
        ArgumentNullException.ThrowIfNull(phrases);
        OcrDataModel screenText = await _vmService.GetScreenTextAsync();
        int matchCount = 0;

        if (screenText == null)
        {
            _logger.LogError("Unable to read screen text");
            return false;
        }

        screenText.Elements.ToList()
            .ForEach(e => e.lines.ToList()
                .ForEach(l => l.words.ToList()
                    .ForEach(p => p.text.ToLower().Split(" ").ToList()
                        .ForEach(w => 
                        {
                            if (phrases.Any(x => x.Split(" ").ToList().Any(y => y.ToLower() == w.ToLower())))
                            {
                                _logger.LogTrace($"Found match -- {w}");
                                matchCount++;
                            }
                        }))));

        decimal confidence = matchCount / (decimal)phrases.Count;

        if (confidence >= matchConfidence)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> WaitForScreen(AtmScreenModel screen, TimeSpan timeout, TimeSpan refreshInterval)
    {
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

        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;

            while (DateTime.Now < endTime)
            {
                if (await SearchForText(phrases, matchConfidence))
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
