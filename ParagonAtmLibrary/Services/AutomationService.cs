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

    public async Task<bool> WaitForScreenText(string text, TimeSpan timeout, TimeSpan? refreshInterval = null)
    {
        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;
            ScreenOcrDataModel screenText;

            while (DateTime.Now < endTime)
            {
                screenText = await _vmService.GetScreenTextAsync();

                if (screenText.Elements.Any(x => x.text.ToLower().Contains(text.ToLower())))
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

    public async Task<bool> WaitForScreenText(string[] text, TimeSpan timeout, TimeSpan? refreshInterval = null, bool matchAll = true)
    {
        try
        {
            DateTime curTime = DateTime.Now;
            DateTime endTime = curTime + timeout;
            ScreenOcrDataModel screenText;

            while (DateTime.Now < endTime)
            {
                screenText = await _vmService.GetScreenTextAsync();

                if (matchAll && text.All(x => screenText.Elements.Any(e => e.text.ToLower().Contains(x.ToLower()))))
                {
                    return true;
                }
                else if (text.Any(x => screenText.Elements.Any(e => e.text.ToLower().Contains(x.ToLower()))))
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
