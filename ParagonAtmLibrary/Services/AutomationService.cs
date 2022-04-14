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
            List<string> result = new();

            screenText.Elements.ToList()
                .ForEach(e => e.lines.ToList()
                    .ForEach(l => l.words.ToList()
                        .ForEach(w => result.Add(w.text))));

            _logger.LogTrace($"GetScreenWords() -- {JsonSerializer.Serialize(result)}");

            return result;
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
                _logger.LogTrace($"IsAtScreen(): Found match -- {s.Name}");
                return true;
            }
        }

        return false;
    }

    public async Task<bool> MatchScreen(List<string> compareWords, decimal matchConfidence)
    {
        List<string> screenText = await GetScreenWords();
        return MatchScreen(screenText, compareWords, matchConfidence);
    }

    public bool MatchScreen(List<string> screenWords, List<string> compareWords, decimal matchConfidence)
    {
        ArgumentNullException.ThrowIfNull(screenWords);
        ArgumentNullException.ThrowIfNull(compareWords);
        ArgumentNullException.ThrowIfNull(matchConfidence);
        int matchCount = 0;

        foreach (string x in screenWords)
        {
            foreach (string y in compareWords)
            {
                List<string> left = x.Split(" ").ToList().Select(w => w.ToLower()).ToList();
                List<string> right = y.Split(" ").ToList().Select(w => w.ToLower()).ToList();

                IEnumerable<string> matches = left.Intersect(right);

                if (matches.Count() > 0)
                {
                    _logger.LogTrace($"MatchScreen(): Word match -- {JsonSerializer.Serialize(matches)}");
                    matchCount += matches.Count();
                }
            }
        }

        int wordCount = 0;

        foreach (string phrase in compareWords)
        {
            wordCount += phrase.Split(" ").Count();
        }

        decimal confidence = matchCount / (decimal)wordCount;

        if (confidence >= matchConfidence)
        {
            _logger.LogTrace($"MatchScreen(): Matched -- {JsonSerializer.Serialize(compareWords)} -- Confidence {confidence}");
            return true;
        }

        _logger.LogTrace($"MatchScreen(): Not match -- {JsonSerializer.Serialize(compareWords)} -- Confidence {confidence}");

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

    public async Task<bool> SearchForText(string[] words, bool matchAll = true)
    {
        ArgumentNullException.ThrowIfNull(words);
        return await SearchForText(words.ToList(), matchAll);
    }

    public async Task<bool> SearchForText(List<string> words, bool matchAll = true)
    {
        ArgumentNullException.ThrowIfNull(words);
        OcrDataModel screenText = await _vmService.GetScreenTextAsync();

        if (screenText == null)
        {
            _logger.LogError("Unable to read screen text");
            return false;
        }
        else if (matchAll)
        {
            return words.All(w => screenText.Elements
                .Any(e => e.lines.ToList()
                    .Any(l => l.words.ToList()
                        .Any((t) =>
                        {
                            if (t.text.Split(" ").ToList().Any(x => x.ToLower().Contains(w.ToLower())))
                            {
                                _logger.LogTrace($"SearchForText() -- Found match -- {t.text}");
                                return t.text.ToLower().Contains(w.ToLower());
                            }

                            return false;
                        }))));
        }
        else
        {
            return words.Any(w => screenText.Elements
                .Any(e => e.lines.ToList()
                    .Any(l => l.words.ToList()
                        .Any((t) =>
                        {
                            if (t.text.Split(" ").ToList().Any(x => x.ToLower().Contains(w.ToLower())))
                            {
                                _logger.LogTrace($"SearchForText() -- Found match -- {t.text}");
                                return t.text.ToLower().Contains(w.ToLower());
                            }

                            return false;
                        }))));
        }
    }

    public async Task<bool> SearchForText(string[] words, decimal matchConfidence)
    {
        ArgumentNullException.ThrowIfNull(words);
        return await SearchForText(words.ToList(), matchConfidence);
    }

    public async Task<bool> SearchForText(List<string> words, decimal matchConfidence)
    {
        ArgumentNullException.ThrowIfNull(words);
        OcrDataModel screenText = await _vmService.GetScreenTextAsync();
        int matchCount = 0;

        if (screenText == null)
        {
            _logger.LogError("Unable to read screen text");
            return false;
        }

        screenText.Elements.ToList()
            .ForEach(e => e.lines.ToList()
                .ForEach(l => l.words.ToList().ForEach(w =>
        {
            if (words.Any(x => x.ToLower() == w.text.ToLower()))
            {
                _logger.LogTrace($"SearchForText() -- Found match {w.text}");
                matchCount++;
            }
        })));

        decimal confidence = matchCount / (decimal)words.Count;

        if (confidence >= matchConfidence)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> WaitForText(string word, TimeSpan timeout, TimeSpan? refreshInterval = null)
    {
        ArgumentNullException.ThrowIfNull(word);
        ArgumentNullException.ThrowIfNull(timeout);

        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;
            OcrDataModel screenText;

            while (DateTime.Now < endTime)
            {
                screenText = await _vmService.GetScreenTextAsync();

                if (screenText == null)
                {
                    _logger.LogError("Unable to read screen text");
                    return false;
                }

                if (screenText.Elements.Any(e => e.text.ToLower().Contains(word.ToLower())))
                {
                    return true;
                }

                if (refreshInterval == null)
                {
                    await Task.Delay(30000);
                }
                else
                {
                    await Task.Delay((TimeSpan)refreshInterval);
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public async Task<bool> WaitForText(string[] words, TimeSpan timeout, TimeSpan? refreshInterval = null, bool matchAll = true)
    {
        return await WaitForText(words?.ToList(), timeout, refreshInterval, matchAll);
    }

    public async Task<bool> WaitForText(List<string> words, TimeSpan timeout, TimeSpan? refreshInterval = null, bool matchAll = true)
    {
        ArgumentNullException.ThrowIfNull(words);
        ArgumentNullException.ThrowIfNull(timeout);

        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;
            OcrDataModel screenText;

            while (DateTime.Now < endTime)
            {
                screenText = await _vmService.GetScreenTextAsync();

                if (screenText == null)
                {
                    _logger.LogError("Unable to read screen text");
                    return false;
                }
                else if (matchAll && words.All(w => screenText.Elements.Any(e => e.text.ToLower().Contains(w.ToLower()))))
                {
                    return true;
                }
                else if (matchAll == false && words.Any(w => screenText.Elements.Any(e => e.text.ToLower().Contains(w.ToLower()))))
                {
                    return true;
                }

                if (refreshInterval == null)
                {
                    await Task.Delay(30000);
                }
                else
                {
                    await Task.Delay((TimeSpan)refreshInterval);
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return false;
        }
    }

    public async Task<bool> WaitForScreen(AtmScreenModel screen, TimeSpan timeout, TimeSpan? refreshInterval = null)
    {
        return await WaitForText(screen.Text, screen.MatchConfidence, timeout, refreshInterval);
    }

    public async Task<bool> WaitForText(string[] words, decimal matchConfidence, TimeSpan timeout, TimeSpan? refreshInterval = null)
    {
        return await WaitForText(words?.ToList(), matchConfidence, timeout, refreshInterval);
    }

    public async Task<bool> WaitForText(List<string> words, decimal matchConfidence, TimeSpan timeout, TimeSpan? refreshInterval = null)
    {
        ArgumentNullException.ThrowIfNull(words);
        ArgumentNullException.ThrowIfNull(matchConfidence);
        ArgumentNullException.ThrowIfNull(timeout);

        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;
            OcrDataModel screenText;

            while (DateTime.Now < endTime)
            {
                screenText = await _vmService.GetScreenTextAsync();
                int matchCount = 0;

                if (screenText == null)
                {
                    _logger.LogError("Unable to read screen text");
                    return false;
                }

                screenText.Elements.ToList()
                    .ForEach(e => e.lines.ToList()
                        .ForEach(l => l.words.ToList().ForEach(w =>
                {
                    if (words.Any(x => x.ToLower() == w.text.ToLower()))
                    {
                        matchCount++;
                    }
                })));

                decimal confidence = matchCount / (decimal)words.Count;

                if (confidence >= matchConfidence)
                {
                    return true;
                }

                if (refreshInterval == null)
                {
                    await Task.Delay(30000);
                }
                else
                {
                    await Task.Delay((TimeSpan)refreshInterval);
                }
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
