﻿namespace VirtualAtmClient;

class Program
{
    public static IConfigurationRoot Configuration { get; set; }

    static async Task Main(string[] args)
    {
        try
        {
            string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            bool isDevelopment = string.IsNullOrEmpty(env) || env.ToLower() == "development";

            await Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appSettings.json", true, true);
                    config.AddJsonFile($"appSettings.{env}.json", true, true);
                    config.AddJsonFile($"Simulation.json", false, true);
                    config.AddJsonFile($"AvailableScreens.json", false, true);
                    config.AddUserSecrets<Program>(optional: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.ClearProviders();
                    builder.AddFileLogger(context.Configuration);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddScoped(sp => new HttpClient(new RequestHandler(new HttpClientHandler(), sp.GetRequiredService<ILogger<RequestHandler>>())));
                    services.AddScoped<IEdgeBalanceInquiryService, EdgeBalanceInquiryService>();
                    services.AddScoped<IVistaBalanceInquiryService, VistaBalanceInquiryService>();
                    services.AddScoped<IActivateEnterpriseBalanceInquiryService, ActivateEnterpriseBalanceInquiryService>();
                    services.AddParagonAtmLibrary();
                    services.AddHostedService<ClientApp>();
                })
                .RunConsoleAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }
}
