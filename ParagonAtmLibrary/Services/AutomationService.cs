using Microsoft.Extensions.Logging;
using ParagonAtmLibrary.Interfaces;
using ParagonAtmLibrary.Models;
using System.Text.Json;

namespace ParagonAtmLibrary.Services;

public class AutomationService : IAutomationService
{
    private readonly ILogger<AgentService> _logger;
    private readonly IVirtualMachineService _vmService;
    private readonly char[] _splitChars;

    public AutomationService(ILogger<AgentService> logger,
                             IVirtualMachineService vmService)
    {
        _logger = logger;
        _vmService = vmService;
        _splitChars = new[] { ' ', ',', '.', '?', ';', ':' };
    }

    /// <summary>
    /// Compares a list of phrases against all words on the screen. In this overload, it's up to the caller
    /// to provide the text of the screen. This can be useful if the caller is doing multiple comparisons, 
    /// but prefers to only grab the screen words only once.
    /// </summary>
    /// <param name="screenWords">List of words on the screen, which the caller can obtain using GetScreenWords().</param>
    /// <param name="comparePhrases">List of phrases, each may contain one or more words for comparison.</param>
    /// <param name="matchConfidence">Required confidence level for any single phrase to be considered a match.</param>
    /// <returns>True, if any phrase matches above the specified confidence level, false otherwise.</returns>
    public bool CompareText(List<string> screenWords, List<string> comparePhrases, decimal matchConfidence)
    {
        ArgumentNullException.ThrowIfNull(screenWords);
        ArgumentNullException.ThrowIfNull(comparePhrases);
        ArgumentNullException.ThrowIfNull(matchConfidence);

        foreach (string phrase in comparePhrases)
        {
            int matchCount = 0;
            decimal confidence = 0;

            IEnumerable<string> matches = screenWords
                .Select(s => s.ToLower())
                .Intersect(phrase.Split(_splitChars)
                    .Select(s => s.Trim().ToLower())
                    .Where(s => string.IsNullOrWhiteSpace(s) == false));

            if (matches.Count() > 0)
            {
                _logger.LogDebug($"Mathcing words -- {JsonSerializer.Serialize(matches)}");
                matchCount += matches.Count();
                confidence = matchCount / (decimal)phrase.Split(_splitChars).Length;

                if (confidence >= matchConfidence)
                {
                    _logger.LogDebug($"Phrase Matched -- {JsonSerializer.Serialize(phrase)} [Confidence {confidence:0.00}] [Required {matchConfidence:0.00}]");
                    return true;
                }
            }

            _logger.LogDebug($"Phrase NotMatched -- {JsonSerializer.Serialize(phrase)} [Confidence {confidence:0.00}] [Required {matchConfidence:0.00}]");
        }

        return false;
    }

    /// <summary>
    /// Compares a list of phrases against all words on the screen. This overload will query the screen of
    /// the ATM to obtain the list of words.
    /// </summary>
    /// <param name="comparePhrases">List of phrases, each may contain one or more words for comparison.</param>
    /// <param name="matchConfidence">Required confidence level for any single phrase to be considered a match.</param>
    /// <returns>True, if any phrase matches above the specified confidence level, false otherwise.</returns>
    public async Task<bool> CompareTextAsync(List<string> comparePhrases, decimal matchConfidence)
    {
        List<string> screenWords = await GetScreenWordsAsync();
        return CompareText(screenWords, comparePhrases, matchConfidence);
    }

    /// <summary>
    /// Finds the location of the specified text and clicks it
    /// </summary>
    /// <param name="findText">The text to find on the screen</param>
    /// <returns>True, if the specified text was located and successfully clicked, false otherwise.</returns>
    public async Task<bool> FindAndClickAsync(string findText)
    {
        LocationModel location = await _vmService.GetLocationByTextAsync(findText);

        if (location is null || location.Found == false)
        {
            return false;
        }

        if (await _vmService.ClickScreenAsync(new ClickScreenModel(location)) == false)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Clicks the first element of the specified text array, that is found
    /// </summary>
    /// <param name="findText">An array of strings to find on the screen</param>
    /// <returns>True, if any of the specified strings were located and clicked, false if none were found</returns>
    public async Task<bool> FindAndClickAsync(string[] findText)
    {
        foreach (string item in findText)
        {
            LocationModel location = await _vmService.GetLocationByTextAsync(item);

            if (location is not null && location.Found)
            {
                if (await _vmService.ClickScreenAsync(new ClickScreenModel(location)))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Simplification of Paragon API method /get-screen-text. This method
    /// flattens the returned result so the consumer doesn't have to 
    /// parse elements, lines and word arrays.
    /// </summary>
    /// <returns>Returns a list of all the words on the ATM screen.</returns>
    public async Task<List<string>> GetScreenWordsAsync()
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

            _logger.LogDebug($"Screen words -- {JsonSerializer.Serialize(result)}");

            return result;
        }
    }

    /// <summary>
    /// Checks if the ATM screen matches the supplied AtmScreenModel. In this overload,
    /// it's up to the caller to provide the text of the screen. This can be useful if
    /// the caller is doing multiple comparisons, but prefers to only grab the screen
    /// words only once.
    /// </summary>
    /// <param name="screen">AtmScreenModel to be checked.</param>
    /// <param name="screenWords">List of words on the screen, which the caller can obtain using GetScreenWords().</param>
    /// <returns>True, if the AtmScreenModel matches above its required confidence level, false otherwise.</returns>
    public bool MatchScreen(AtmScreenModel screen, List<string> screenWords)
    {
        return CompareText(screenWords, screen.Text, screen.MatchConfidence);
    }

    /// <summary>
    /// Checks if the ATM screen matches with any from a List of AtmScreenModels. 
    /// In this overload, it's up to the caller to provide the text of the screen.
    /// This can be useful if the caller is doing multiple comparisons, but 
    /// prefers to only grab the screen words only once.
    /// </summary>
    /// <param name="screens">List of AtmScreenModels to check.</param>
    /// <param name="screenWords">List of words on the screen, which the caller can obtain using GetScreenWords().</param></param>
    /// <returns>If matched, returns the matching AtmScreenModel, null otherwise.</returns>
    public AtmScreenModel MatchScreen(List<AtmScreenModel> screens, List<string> screenWords)
    {
        foreach (AtmScreenModel s in screens)
        {
            if (CompareText(screenWords, s.Text, s.MatchConfidence))
            {
                _logger.LogDebug($"Found match -- {s.Name}");
                return s;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the ATM screen matches the supplied AtmScreenModel. This overload 
    /// will query the screen of the ATM to obtain the list of words.
    /// </summary>
    /// <param name="screen">AtmScreenModel to be checked.</param>
    /// <returns>True, if the AtmScreenModel matches above its required confidence level, false otherwise.</returns>
    public async Task<bool> MatchScreenAsync(AtmScreenModel screen)
    {
        List<string> screenText = await GetScreenWordsAsync();
        return CompareText(screenText, screen.Text, screen.MatchConfidence);
    }

    /// <summary>
    /// Checks if the ATM screen matches with any in the specified list. This overload 
    /// will query the screen of the ATM to obtain the list of words.
    /// </summary>
    /// <param name="screens">List of AtmScreenModels to check.</param>
    /// <returns>If matched, returns the matching AtmScreenModel, null otherwise.</returns>
    public async Task<AtmScreenModel> MatchScreenAsync(List<AtmScreenModel> screens)
    {
        List<string> screenWords = await GetScreenWordsAsync();
        return MatchScreen(screens, screenWords);
    }

    /// <summary>
    /// Waits for the specified AtmScreenModel to be displayed on screen.
    /// </summary>
    /// <param name="screen">The AtmScreenModel to wait for.</param>
    /// <param name="timeout">The overall timeout to wait for this screen match.</param>
    /// <param name="refreshInterval">How often to refresh the screen OCR data to check for a match.</param>
    /// <returns>Returns true, if the specified AtmScreenModel matches above it's required confidence level, and within the specified timeout, false otherwise.</returns>
    public async Task<bool> WaitForScreenAsync(AtmScreenModel screen, TimeSpan timeout, TimeSpan refreshInterval)
    {
        _logger.LogDebug($"Wait for screen -- {screen.Name}");
        return await WaitForTextAsync(screen.Text, screen.MatchConfidence, timeout, refreshInterval);
    }

    /// <summary>
    /// Waits for the specified text to be displayed on the ATM screen. This overload accepts
    /// a string list for input.
    /// </summary>
    /// <param name="phrases">List of phrases to wait for.</param>
    /// <param name="matchConfidence">Required confidence level the words must match with the screen words.</param>
    /// <param name="timeout">The overall timeout to wait for this screen match.</param>
    /// <param name="refreshInterval">How often to refresh the screen OCR data to check for a match.</param>
    /// <returns>Returns true, if the specified word list matches above the specified confidence level, and within the specified timeout, false otherwise.</returns>
    public async Task<bool> WaitForTextAsync(List<string> phrases, decimal matchConfidence, TimeSpan timeout, TimeSpan refreshInterval)
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
                if (await CompareTextAsync(phrases, matchConfidence))
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

    /// <summary>
    /// Waits for the specified text to be displayed on the ATM screen. This overload accepts
    /// a string array for input.
    /// </summary>
    /// <param name="phrases">Array of phrases to wait for.</param>
    /// <param name="matchConfidence">Required confidence level the words must match with the screen words.</param>
    /// <param name="timeout">The overall timeout to wait for this screen match.</param>
    /// <param name="refreshInterval">How often to refresh the screen OCR data to check for a match.</param>
    /// <returns>Returns true, if the specified word array matches above the specified confidence level, and within the specified timeout, false otherwise.</returns>
    public async Task<bool> WaitForTextAsync(string[] phrases, decimal matchConfidence, TimeSpan timeout, TimeSpan refreshInterval)
    {
        return await WaitForTextAsync(phrases.ToList(), matchConfidence, timeout, refreshInterval);
    }
}
