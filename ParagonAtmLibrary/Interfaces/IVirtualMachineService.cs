namespace ParagonAtmLibrary.Interfaces;

public interface IVirtualMachineService
{
    Task<bool> ClickScreenAsync(ClickScreenModel clickPoint, bool rightClick = false);
    Task<List<ScreenModel>> EnumerateScreensAsync();
    Task<LocationModel> GetLocationByTextAsync(string inputText);
    Task<string> GetScreenAsync(string screenName = "");
    Task<string> GetScreenJpegAsync(string screenName = "");
    Task<OcrDataModel> GetScreenTextAsync(string screenName = "");
}