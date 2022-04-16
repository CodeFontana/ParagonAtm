using ParagonAtmLibrary.Models;

namespace ParagonAtmLibrary.Services;
public interface IAutomationService
{
    bool CompareText(List<string> screenWords, List<string> comparePhrases, decimal matchConfidence);
    Task<bool> CompareTextAsync(List<string> comparePhrases, decimal matchConfidence);
    Task<List<string>> GetScreenWordsAsync();
    bool MatchScreen(AtmScreenModel screen, List<string> screenWords);
    AtmScreenModel MatchScreen(List<AtmScreenModel> screens, List<string> screenWords);
    Task<bool> MatchScreenAsync(AtmScreenModel screen);
    Task<AtmScreenModel> MatchScreenAsync(List<AtmScreenModel> screens);
    Task<bool> WaitForScreenAsync(AtmScreenModel screen, TimeSpan timeout, TimeSpan refreshInterval);
    Task<bool> WaitForTextAsync(List<string> phrases, decimal matchConfidence, TimeSpan timeout, TimeSpan refreshInterval);
    Task<bool> WaitForTextAsync(string[] phrases, decimal matchConfidence, TimeSpan timeout, TimeSpan refreshInterval);
}