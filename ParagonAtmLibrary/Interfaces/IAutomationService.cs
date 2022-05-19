﻿using ParagonAtmLibrary.Models;

namespace ParagonAtmLibrary.Interfaces;
public interface IAutomationService
{
    bool CompareText(List<string> screenWords, List<string> comparePhrases, decimal matchConfidence, int acceptableEditDistance = 0);
    Task<bool> CompareTextAsync(List<string> comparePhrases, decimal matchConfidence, int acceptableEditDistance = 0);
    Task<bool> FindAndClickAsync(string findText);
    Task<bool> FindAndClickAsync(string[] findText);
    Task<List<string>> GetScreenWordsAsync();
    bool MatchScreen(AtmScreenModel screen, List<string> screenWords);
    AtmScreenModel MatchScreen(List<AtmScreenModel> screens, List<string> screenWords);
    Task<bool> MatchScreenAsync(AtmScreenModel screen);
    Task<AtmScreenModel> MatchScreenAsync(List<AtmScreenModel> screens);
    Task<bool> WaitForScreenAsync(AtmScreenModel screen, TimeSpan timeout, TimeSpan refreshInterval);
    Task<AtmScreenModel> WaitForScreensAsync(List<AtmScreenModel> screens, TimeSpan timeout, TimeSpan refreshInterval);
    Task<bool> WaitForTextAsync(List<string> phrases, decimal matchConfidence, TimeSpan timeout, TimeSpan refreshInterval, int acceptableEditDistance = 0);
    Task<bool> WaitForTextAsync(string[] phrases, decimal matchConfidence, TimeSpan timeout, TimeSpan refreshInterval, int acceptableEditDistance = 0);
}