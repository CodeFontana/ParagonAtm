namespace ParagonAtmLibrary.Services;

public class VirtualMachineService : IVirtualMachineService
{
    private readonly IConfiguration _config;
    private readonly ILogger<VirtualMachineService> _logger;
    private readonly HttpClient _httpClient;

    public VirtualMachineService(IConfiguration configuration,
                                 ILogger<VirtualMachineService> logger,
                                 HttpClient httpClient)
    {
        _config = configuration;
        _logger = logger;
        _httpClient = httpClient;
    }

    public record OcrData(OcrDataModel ocrData);

    public async Task<string> GetScreenAsync(string screenName = "")
    {
        try
        {
            _logger.LogInformation("Get screen...");

            ScreenNameModel screen = new()
            {
                ScreenName = screenName
            };

            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:VirtualMachine"]}/get-screen", screen);

            if (response.IsSuccessStatusCode)
            {
                ScreenshotModel result = await response.Content.ReadFromJsonAsync<ScreenshotModel>();
                _logger.LogInformation($"Success -- Base64/{result.Result.Substring(0, 50)}...");
                return result.Result;
            }
            else
            {
                _logger.LogError($"Failed to get screen -- [{response.ReasonPhrase}]");
                return null;
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to get screen -- [{e.Message}]");
            return null;
        }
    }

    public async Task<string> GetScreenJpegAsync(string screenName = "")
    {
        try
        {
            _logger.LogInformation("Get screen JPEG...");

            ScreenNameModel screen = new()
            {
                ScreenName = screenName
            };

            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:VirtualMachine"]}/get-screen-jpeg", screen);

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

    public async Task<OcrDataModel> GetScreenTextAsync(string screenName = "")
    {
        try
        {
            _logger.LogInformation($"Get screen text...");

            ScreenNameModel screen = new()
            {
                ScreenName = screenName
            };

            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync($"{_config["ApiEndpoint:VirtualMachine"]}/get-screen-text", screen);

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

    public async Task<LocationModel> GetLocationByTextAsync(string inputText)
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
                LocationModel result = await response.Content.ReadFromJsonAsync<LocationModel>();
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
