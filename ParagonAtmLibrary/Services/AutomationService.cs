namespace ParagonAtmLibrary.Services;

public class AutomationService : IAutomationService
{
    private readonly ILogger<AutomationService> _logger;
    private readonly IVirtualMachineService _vmService;
    private readonly char[] _splitChars;

    public AutomationService(ILogger<AutomationService> logger,
                             IVirtualMachineService vmService)
    {
        _logger = logger;
        _vmService = vmService;
        _splitChars = new[] { ' ', ',', '.', '?', ';', ':', '\n' };
    }

    /// <summary>
    /// Compares a phrase against all words on the screen. In this overload, it's up to the caller to
    /// provide the text of the screen. This can be useful if the caller is doing multiple comparisons, 
    /// but prefers to only grab the screen words only once.
    /// </summary>
    /// <param name="screenWords">List of words on the screen, which the caller can obtain using GetScreenWords().</param>
    /// <param name="phraseText">Phrase for comparison.</param>
    /// <param name="matchConfidence">Required confidence level for any single phrase to be considered a match.</param>
    /// <param name="acceptableEditDistance">Acceptable edit distance when comparing words for equality.</param>
    /// <returns>True, if the phrase matches above the specified confidence level, false otherwise.</returns>
    public bool CompareText(List<string> screenWords, string phraseText, decimal matchConfidence, int acceptableEditDistance = 0)
    {
        ArgumentNullException.ThrowIfNull(screenWords);
        ArgumentNullException.ThrowIfNull(phraseText);
        ArgumentNullException.ThrowIfNull(matchConfidence);

        decimal confidence = 0;
        List<string> matches = new();

        foreach (string pWord in phraseText.Split(_splitChars))
        {
            foreach (string sWord in screenWords)
            {
                if (sWord.ToLower().Trim() == pWord.ToLower().Trim())
                {
                    matches.Add(pWord);
                    break;
                }
                else if (acceptableEditDistance > 0
                    && ComputeEditDistance(sWord.ToLower().Trim(), pWord.ToLower().Trim()) <= acceptableEditDistance)
                {
                    matches.Add(pWord);
                    break;
                }
            }
        }

        if (matches.Count > 0)
        {
            _logger.LogDebug($"Mathcing words -- {JsonSerializer.Serialize(matches)}");
            confidence = matches.Count / (decimal)phraseText.Split(_splitChars).Length;

            if (confidence >= matchConfidence)
            {
                _logger.LogDebug($"Phrase Matched -- {JsonSerializer.Serialize(phraseText)} [Confidence {confidence:0.00}] [Required {matchConfidence:0.00}]");
                return true;
            }
        }

        _logger.LogDebug($"Phrase NotMatched -- {JsonSerializer.Serialize(phraseText)} [Confidence {confidence:0.00}] [Required {matchConfidence:0.00}]");

        return false;
    }

    /// <summary>
    /// Compares a phrase against all words on the screen. This overload will query the screen of
    /// the ATM to obtain the list of words.
    /// </summary>
    /// <param name="phraseText">List of phrases, each may contain one or more words for comparison.</param>
    /// <param name="matchConfidence">Required confidence level for any single phrase to be considered a match.</param>
    /// <param name="acceptableEditDistance">Acceptable edit distance when comparing words for equality.</param>
    /// <returns>True, if the phrase matches above the specified confidence level, false otherwise.</returns>
    public async Task<bool> CompareTextAsync(string phraseText, decimal matchConfidence, int acceptableEditDistance = 0)
    {
        List<string> screenWords = await GetScreenWordsAsync();
        return CompareText(screenWords, phraseText, matchConfidence, acceptableEditDistance);
    }

    /// <summary>
    /// Find minimum number of edits (operations) required to convert 'left' into 'right'.
    /// </summary>
    /// <param name="left">String for comparison</param>
    /// <param name="right">String for comparison</param>
    /// <returns>An integer representing the edit distance or character difference between the two strings.</returns>
    private static int ComputeEditDistance(string left, string right)
    {
        if (left.Length == 0)
        {
            return right.Length;
        }

        if (right.Length == 0)
        {
            return left.Length;
        }

        var d = new int[left.Length + 1, right.Length + 1];

        for (int i = 0; i <= left.Length; i++)
        {
            d[i, 0] = i;
        }

        for (int j = 0; j <= right.Length; j++)
        {
            d[0, j] = j;
        }

        for (int i = 1; i <= left.Length; i++)
        {
            for (int j = 1; j <= right.Length; j++)
            {
                int cost = (right[j - 1] == left[i - 1]) ? 0 : 1;

                d[i, j] = ComputeMin(
                    d[i - 1, j] + 1,
                    d[i, j - 1] + 1,
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[left.Length, right.Length];
    }

    /// <summary>
    /// Computes the midpoint between two points.
    /// </summary>
    /// <param name="x0">X coordinate of first point</param>
    /// <param name="y0">Y coordinate of first point</param>
    /// <param name="x1">X coordinate of second point</param>
    /// <param name="y1">Y coordinate of second point</param>
    /// <returns>Screen coordinates representing the midpoint between the two points.</returns>
    private static ScreenCoordinates ComputeMidpoint(float x0, float y0, float x1, float y1)
    {
        ScreenCoordinates result = new();
        result.x = (x0 + x1) / 2;
        result.y = (y0 + y1) / 2;
        return result;
    }

    /// <summary>
    /// Computes the minimum value of the inputs.
    /// </summary>
    /// <param name="e1">First input</param>
    /// <param name="e2">Second input</param>
    /// <param name="e3">Third input</param>
    /// <returns>The minimum of the three values</returns>
    private static int ComputeMin(int e1, int e2, int e3) => Math.Min(Math.Min(e1, e2), e3);

    /// <summary>
    /// Finds the location of the specified text and clicks it. If matching text is found
    /// in multiple locations, the location of the best match is chosen based on a weighted
    /// algorithm.
    /// </summary>
    /// <param name="findText">The text to find on the screen</param>
    /// <param name="acceptableEditDistance">Acceptable edit distance when comparing words for equality.</param>
    /// <returns>True, if the specified text was located and successfully clicked, false otherwise.</returns>
    public async Task<bool> FindAndClickAsync(string findText, int acceptableEditDistance = 0)
    {
        OcrDataModel screenText = await _vmService.GetScreenTextAsync();
        List<FindAndClickModel> textMatches = new();

        if (screenText == null)
        {
            _logger.LogError("Unable to read screen text");
            return false;
        }

        _logger.LogInformation($"Find text: {findText}");
        string[] findWords = findText.ToLower().Split(_splitChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (Element element in screenText.Elements)
        {
            if (string.IsNullOrEmpty(element.text))
            {
                continue;
            }
            
            int wordMatchCount = 0;
            string[] elementWords = element.text.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (string eWord in elementWords)
            {
                if (findWords.Contains(eWord.ToLower()))
                {
                    wordMatchCount++;
                }
                else if (acceptableEditDistance > 0
                        && findWords.Any(f => ComputeEditDistance(eWord.ToLower(), f) <= acceptableEditDistance))
                {
                    wordMatchCount++;
                }
            }

            if (wordMatchCount > 0)
            {
                decimal elementConfidence = wordMatchCount / (decimal)Math.Max(findWords.Length, elementWords.Length);
                ScreenCoordinates midPoint = ComputeMidpoint(element.x0, element.y0, element.x1, element.y1);
                _logger.LogDebug($"Element match -- {element.text} [x:{midPoint.x} y:{midPoint.y}] [Confidence {elementConfidence:0.00}]");
                textMatches.Add(new FindAndClickModel(midPoint, element.text, elementConfidence));

                if (elementConfidence < 1)
                {
                    foreach (Line line in element.lines)
                    {
                        if (string.IsNullOrEmpty(line.text))
                        {
                            continue;
                        }

                        wordMatchCount = 0;
                        string[] lineWords = line.text.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                        foreach (string lWord in lineWords)
                        {
                            if (findWords.Contains(lWord.ToLower()))
                            {
                                wordMatchCount++;
                            }
                            else if (acceptableEditDistance > 0
                                    && findWords.Any(f => ComputeEditDistance(lWord.ToLower(), f) <= acceptableEditDistance))
                            {
                                wordMatchCount++;
                            }
                        }

                        if (wordMatchCount > 0)
                        {
                            decimal lineConfidence = wordMatchCount / (decimal)Math.Max(findWords.Length, lineWords.Length);

                            midPoint = ComputeMidpoint(line.x0, line.y0, line.x1, line.y1);
                            _logger.LogDebug($"Line match -- {string.Join(' ', lineWords)} [x:{midPoint.x} y:{midPoint.y}] [Confidence {lineConfidence:0.00}]");
                            textMatches.Add(new FindAndClickModel(midPoint, line.text, lineConfidence));

                            if (lineConfidence < 1)
                            {
                                foreach (Word word in line.words)
                                {
                                    wordMatchCount = 0;
                                    string[] words = word.text.Split(_splitChars, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                                    foreach (string w in words)
                                    {
                                        if (findWords.Contains(w.ToLower()))
                                        {
                                            wordMatchCount++;
                                        }
                                        else if (acceptableEditDistance > 0
                                                && findWords.Any(f => ComputeEditDistance(w.ToLower().Trim(), f) <= acceptableEditDistance))
                                        {
                                            wordMatchCount++;
                                        }
                                    }

                                    if (wordMatchCount > 0)
                                    {
                                        decimal wordConfidence = wordMatchCount / (decimal)Math.Max(findWords.Length, words.Length) * lineConfidence;
                                        midPoint = ComputeMidpoint(word.x0, word.y0, word.x1, word.y1);
                                        _logger.LogDebug($"Word match -- {word.text} [x:{midPoint.x} y:{midPoint.y}] [Confidence {wordConfidence:0.00}]");
                                        textMatches.Add(new FindAndClickModel(midPoint, word.text, wordConfidence));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (textMatches.Count == 0)
        {
            _logger.LogError($"Text not found -- {findText}");
            return false;
        }

        ScreenCoordinates location = textMatches
            .First(tm => tm.Confidence == textMatches.Max(m => m.Confidence))
            .Location;

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
        foreach (ScreenPhrase phrase in screen.Phrases)
        {
            if (CompareText(screenWords, phrase.Text, phrase.MatchConfidence, phrase.EditDistance))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if the ATM screen matches with any from a List of AtmScreenModels. 
    /// In this overload, it's up to the caller to provide the text of the screen.
    /// This can be useful if the caller is doing multiple comparisons, but 
    /// prefers to only grab the screen words only once.
    /// </summary>
    /// <param name="screens">List of AtmScreenModels to check.</param>
    /// <param name="screenWords">List of words on the screen, which the caller can obtain using GetScreenWords().</param>
    /// <returns>If matched, returns the matching AtmScreenModel, null otherwise.</returns>
    public AtmScreenModel MatchScreen(List<AtmScreenModel> screens, List<string> screenWords)
    {
        foreach (AtmScreenModel s in screens)
        {
            foreach (ScreenPhrase p in s.Phrases)
            {
                if (CompareText(screenWords, p.Text, p.MatchConfidence, p.EditDistance))
                {
                    return s;
                }
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
        return MatchScreen(screen, screenText);
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
        _logger.LogInformation($"Wait for screen -- {screen.Name}");

        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;

            while (DateTime.Now < endTime)
            {
                if (await MatchScreenAsync(screen))
                {
                    return true;
                }

                await Task.Delay(refreshInterval);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error waiting for ATM screen -- {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Waits for any of the specified AtmScreenModels to be displayed on the screen.
    /// </summary>
    /// <param name="screens">List of AtmScreenModels to wait for.</param>
    /// <param name="timeout">The overall timeout to wait for this screen match.</param>
    /// <param name="refreshInterval">How often to refresh the screen OCR data to check for a match.</param>
    /// <returns>Returns the AtmScreenModel from the supplied list that matches the current screen, null if no match is found after the specified timeout.</returns>
    public async Task<AtmScreenModel> WaitForScreensAsync(List<AtmScreenModel> screens, TimeSpan timeout, TimeSpan refreshInterval)
    {
        _logger.LogInformation($"Wait for screens -- {string.Join(",", screens.Select(s => s.Name))}");

        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;

            while (DateTime.Now < endTime)
            {
                foreach (AtmScreenModel screenModel in screens)
                {
                    if (await MatchScreenAsync(screenModel))
                    {
                        return screenModel;
                    }
                }

                await Task.Delay(refreshInterval);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error waiting for ATM screen -- {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Waits for the specified text to be displayed on screen.
    /// </summary>
    /// <param name="phrase">The text to wait for.</param>
    /// <param name="matchConfidence">Required confidence level for any single phrase to be considered a match.</param>
    /// <param name="timeout">The overall timeout to wait for this screen match.</param>
    /// <param name="refreshInterval">How often to refresh the screen OCR data to check for a match.</param>
    /// <param name="acceptableEditDistance">Acceptable edit distance when comparing words for equality.</param>
    /// <returns>Returns true, if the specified phrase matches above it's required confidence level, and within the specified timeout, false otherwise.</returns>
    public async Task<bool> WaitForTextAsync(string phrase, decimal matchConfidence, TimeSpan timeout, TimeSpan refreshInterval, int acceptableEditDistance = 0)
    {
        _logger.LogInformation($"Wait for text -- {phrase}");

        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;

            while (DateTime.Now < endTime)
            {
                if (await CompareTextAsync(phrase, matchConfidence, acceptableEditDistance))
                {
                    return true;
                }

                await Task.Delay(refreshInterval);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error waiting for text -- {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Waits for any of the specified phrases to be displayed on screen.
    /// </summary>
    /// <param name="phrases">List of phrases to wait for.</param>
    /// <param name="matchConfidence">Required confidence level for any single phrase to be considered a match.</param>
    /// <param name="timeout">The overall timeout to wait for this screen match.</param>
    /// <param name="refreshInterval">How often to refresh the screen OCR data to check for a match.</param>
    /// <param name="acceptableEditDistance">Acceptable edit distance when comparing words for equality.</param>
    /// <returns>Returns true, if any of the specified phrases matches above it's required confidence level, and within the specified timeout, false otherwise.</returns>
    public async Task<bool> WaitForTextAsync(List<string> phrases, decimal matchConfidence, TimeSpan timeout, TimeSpan refreshInterval, int acceptableEditDistance = 0)
    {
        _logger.LogInformation($"Wait for text -- {string.Join(" | ", phrases)}");

        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;

            while (DateTime.Now < endTime)
            {
                foreach (string s in phrases)
                {
                    if (await CompareTextAsync(s, matchConfidence, acceptableEditDistance))
                    {
                        return true;
                    }
                }

                await Task.Delay(refreshInterval);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error waiting for text -- {ex.Message}");
            return false;
        }
    }
}
