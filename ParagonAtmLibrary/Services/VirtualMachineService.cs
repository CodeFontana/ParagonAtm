using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;
using ParagonAtmLibrary.Models;

namespace ParagonAtmLibrary.Services;

public class VirtualMachineService
{
    private readonly IConfiguration _config;
    private readonly ILogger<VirtualMachineService> _logger;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public VirtualMachineService(IConfiguration configuration,
                                 ILogger<VirtualMachineService> logger,
                                 HttpClient httpClient)
    {
        _config = configuration;
        _logger = logger;
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    public record OcrData(ScreenOcrDataModel ocrData);

    public async Task<string> GetScreenJpegAsync()
    {
        try
        {
            _logger.LogInformation("Get screen JPEG...");

            using HttpResponseMessage response =
                await _httpClient.PostAsync($"{_config["ApiEndpoint:VirtualMachine"]}/get-screen-jpeg", null);

            if (response.IsSuccessStatusCode)
            {
                ScreenshotModel result = await response.Content.ReadFromJsonAsync<ScreenshotModel>();
                _logger.LogInformation($"Success -- Base64/{result.Result.Substring(0, 50)}...");
                return result.Result;
            }
            else
            {
                _logger.LogError($"Failed to get screen jpeg -- [{response.ReasonPhrase}]");
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to get screen jpeg -- [{e.Message}]");
            return null;
        }
    }

    public async Task<ScreenOcrDataModel> GetScreenTextAsync()
    {
        try
        {
            _logger.LogInformation($"Get screen text...");

            using HttpResponseMessage response =
                await _httpClient.PostAsync($"{_config["ApiEndpoint:VirtualMachine"]}/get-screen-text", null);

            if (response.IsSuccessStatusCode)
            {
                OcrData result = await response.Content.ReadFromJsonAsync<OcrData>();
                return result.ocrData;
            }
            else
            {
                _logger.LogError($"Failed to get screen text -- [{response.ReasonPhrase}]");
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to get screen text -- [{e.Message}]");
            return null;
        }
    }

    public async Task<bool> ClickScreenAsync(ClickScreenModel clickPoint, bool rightClick = false)
    {
        _logger.LogInformation($"Click screen [x:{clickPoint.XCoordinate}, y:{clickPoint.YCoordinate}]...");

        if (rightClick)
        {
            return await VirtualMachineRequestAsync("right-click-screen", clickPoint);
        }
        else
        {
            return await VirtualMachineRequestAsync("click-screen", clickPoint);
        }
    }

    public async Task<ScreenTextLocationModel> GetLocationByTextAsync(string inputText)
    {
        try
        {
            _logger.LogInformation($"Get location of [\"{inputText}\"]...");

            var content = new
            {
                text = inputText
            };

            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:VirtualMachine"]}/get-location-by-text", content);

            if (response.IsSuccessStatusCode)
            {
                ScreenTextLocationModel result = await response.Content.ReadFromJsonAsync<ScreenTextLocationModel>();
                return result;
            }
            else
            {
                _logger.LogError($"Failed to get text location -- [{response.ReasonPhrase}]");
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to get text location -- [{e.Message}]");
            return null;
        }
    }

    private async Task<bool> VirtualMachineRequestAsync<T>(string endpoint, T content)
    {
        try
        {
            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:VirtualMachine"]}/{endpoint}", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Success [{response.StatusCode}] -- /{endpoint}");
                return true;
            }
            else
            {
                _logger.LogError($"Request failed [{response.StatusCode}] [/{endpoint}] -- {response.ReasonPhrase}");
                return false;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Request failed [/{endpoint}] -- {e.Message}");
            return false;
        }
    }
}
