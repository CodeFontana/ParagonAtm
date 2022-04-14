﻿using FileLoggerLibrary;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParagonAtmLibrary;
using VirtualAtmClient.Helpers;

namespace VirtualAtmClient;

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
                    config.AddJsonFile("appsettings.json", true, true);
                    config.AddJsonFile($"appsettings.{env}.json", true, true);
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