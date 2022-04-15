using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Models;
using System.Text.Json;

namespace ParagonAtmLibrary.Services;

public class AutomationService
{
    private readonly ILogger<AgentService> _logger;
    private readonly VirtualMachineService _vmService;
    private readonly char[] _splitChars;

    public AutomationService(ILogger<AgentService> logger,
                             AtmService atmService,
                             VirtualMachineService vmService)
    {
        _logger = logger;
        _vmService = vmService;
        _splitChars = new[] { ' ', ',', '.', '?', ';', ':' };
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

    public async Task<AtmScreenModel> IsAtScreen(List<AtmScreenModel> screens)
    {
        List<string> screenText = await GetScreenWords();
        return IsAtScreen(screens, screenText);
    }

    public AtmScreenModel IsAtScreen(List<AtmScreenModel> screens, List<string> screenText)
    {
        foreach (AtmScreenModel s in screens)
        {
            if (CompareText(screenText, s.Text, s.MatchConfidence))
            {
                _logger.LogTrace($"Found match -- {s.Name}");
                return s;
            }
        }

        return null;
    }

    public bool IsAtScreen(AtmScreenModel screen, List<string> screenText)
    {
        return CompareText(screenText, screen.Text, screen.MatchConfidence);
    }

    public async Task<bool> IsAtScreen(AtmScreenModel screen)
    {
        List<string> screenText = await GetScreenWords();
        return CompareText(screenText, screen.Text, screen.MatchConfidence);
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
