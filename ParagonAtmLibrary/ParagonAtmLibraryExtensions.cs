using Microsoft.Extensions.DependencyInjection;
using ParagonAtmLibrary.Interfaces;
using ParagonAtmLibrary.Services;

namespace ParagonAtmLibrary;

public static class ParagonAtmLibraryExtensions
{
    public static IServiceCollection AddParagonAtmLibrary(this IServiceCollection services)
    {
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IConnectionService, ConnectionService>();
        services.AddScoped<IVirtualMachineService, VirtualMachineService>();
        services.AddScoped<IAtmService, AtmService>();
        services.AddScoped<IAutomationService, AutomationService>();
        return services;
    }
}
