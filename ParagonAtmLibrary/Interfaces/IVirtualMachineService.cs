using ParagonAtmLibrary.Models;

namespace ParagonAtmLibrary.Interfaces;
public interface IVirtualMachineService
{
    Task<bool> ClickScreenAsync(ClickScreenModel clickPoint, bool rightClick = false);
    Task<LocationModel> GetLocationByTextAsync(string inputText);
    Task<string> GetScreenJpegAsync();
    Task<OcrDataModel> GetScreenTextAsync();
}