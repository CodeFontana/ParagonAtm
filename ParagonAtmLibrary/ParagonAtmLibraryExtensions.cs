using Microsoft.Extensions.DependencyInjection;
using ParagonAtmLibrary.Services;

namespace ParagonAtmLibrary;

public static class ParagonAtmLibraryExtensions
{
    public static IServiceCollection AddParagonAtmLibrary(this IServiceCollection services)
    {
        services.AddScoped<AgentService>();
        services.AddScoped<ConnectionService>();
        services.AddScoped<VirtualMachineService>();
        services.AddScoped<AtmService>();
        return services;
    }
}
