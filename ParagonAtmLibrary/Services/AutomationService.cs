using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Models;

namespace ParagonAtmLibrary.Services;

public class AutomationService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AgentService> _logger;
    private readonly AtmService _atmService;
    private readonly VirtualMachineService _vmService;

    public AutomationService(IConfiguration config,
                             ILogger<AgentService> logger,
                             AtmService atmService,
                             VirtualMachineService vmService)
    {
        _config = config;
        _logger = logger;
        _atmService = atmService;
        _vmService = vmService;
    }

    public async Task<List<string>> GetScreenWords()
    {
        ScreenOcrDataModel screenText = await _vmService.GetScreenTextAsync();

        if (screenText == null)
        {
            _logger.LogError("Unable to read screen text");
            return null;
        }
        else
        {
            List<string> result = new();
            screenText.Elements.ToList().ForEach(e => e.words.ToList().ForEach(w => result.Add(w.text)));
            return result;
        }
    }

    public bool IsScreen(List<string> screenText, List<string> compareText, decimal confidence)
    {
        int count = screenText.Intersect(compareText, StringComparer.OrdinalIgnoreCase).Count();

        if (count / compareText.Count >= confidence)
        {
            return true;
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
        ScreenOcrDataModel screenText = await _vmService.GetScreenTextAsync();

        if (screenText == null)
        {
            _logger.LogError("Unable to read screen text");
            return false;
        }
        else if (matchAll)
        {
            return words.All(w => screenText.Elements.Any(e => e.text.ToLower().Contains(w.ToLower())));
        }
        else
        {
            return words.Any(w => screenText.Elements.Any(e => e.text.ToLower().Contains(w.ToLower())));
        }
    }

    public async Task<bool> SearchForText(List<string> words, bool matchAll = true)
    {
        ScreenOcrDataModel screenText = await _vmService.GetScreenTextAsync();

        if (screenText == null)
        {
            _logger.LogError("Unable to read screen text");
            return false;
        }
        else if (matchAll)
        {
            return words.All(w => screenText.Elements.Any(e => e.text.ToLower().Contains(w.ToLower())));
        }
        else
        {
            return words.Any(w => screenText.Elements.Any(e => e.text.ToLower().Contains(w.ToLower())));
        }
    }

    public async Task<bool> SearchForText(string[] words, decimal matchConfidence)
    {
        ScreenOcrDataModel screenText = await _vmService.GetScreenTextAsync();
        int matchCount = 0;

        if (screenText == null)
        {
            _logger.LogError("Unable to read screen text");
            return false;
        }

        screenText.Elements.ToList().ForEach(e => e.words.ToList().ForEach(w =>
        {
            if (words.Any(x => x.ToLower() == w.text.ToLower()))
            {
                matchCount++;
            }
        }));

        if (matchCount / words.Length >= matchConfidence)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> SearchForText(List<string> words, decimal matchConfidence)
    {
        ScreenOcrDataModel screenText = await _vmService.GetScreenTextAsync();
        int matchCount = 0;

        if (screenText == null)
        {
            _logger.LogError("Unable to read screen text");
            return false;
        }

        screenText.Elements.ToList().ForEach(e => e.words.ToList().ForEach(w =>
        {
            if (words.Any(x => x.ToLower() == w.text.ToLower()))
            {
                matchCount++;
            }
        }));

        if (matchCount / words.Count >= matchConfidence)
        {
            return true;
        }

        return false;
    }

    public async Task<bool> WaitForScreenText(string word, TimeSpan timeout, TimeSpan? refreshInterval = null)
    {
        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;
            ScreenOcrDataModel screenText;

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

    public async Task<bool> WaitForScreenText(string[] words, TimeSpan timeout, TimeSpan? refreshInterval = null, bool matchAll = true)
    {
        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;
            ScreenOcrDataModel screenText;

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

    public async Task<bool> WaitForScreenText(List<string> words, TimeSpan timeout, TimeSpan? refreshInterval = null, bool matchAll = true)
    {
        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;
            ScreenOcrDataModel screenText;

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

    public async Task<bool> WaitForScreenText(string[] words, decimal matchConfidence, TimeSpan timeout, TimeSpan? refreshInterval = null)
    {
        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;
            ScreenOcrDataModel screenText;

            while (DateTime.Now < endTime)
            {
                screenText = await _vmService.GetScreenTextAsync();
                int matchCount = 0;

                if (screenText == null)
                {
                    _logger.LogError("Unable to read screen text");
                    return false;
                }

                screenText.Elements.ToList().ForEach(e => e.words.ToList().ForEach(w =>
                {
                    if (words.Any(x => x.ToLower() == w.text.ToLower()))
                    {
                        matchCount++;
                    }
                }));

                if (matchCount / words.Length >= matchConfidence)
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

    public async Task<bool> WaitForScreenText(List<string> words, decimal matchConfidence, TimeSpan timeout, TimeSpan? refreshInterval = null)
    {
        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;
            ScreenOcrDataModel screenText;

            while (DateTime.Now < endTime)
            {
                screenText = await _vmService.GetScreenTextAsync();
                int matchCount = 0;

                if (screenText == null)
                {
                    _logger.LogError("Unable to read screen text");
                    return false;
                }

                screenText.Elements.ToList().ForEach(e => e.words.ToList().ForEach(w =>
                {
                    if (words.Any(x => x.ToLower() == w.text.ToLower()))
                    {
                        matchCount++;
                    }
                }));

                if (matchCount / words.Count >= matchConfidence)
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
