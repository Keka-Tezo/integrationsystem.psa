using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using oculusit.sync.core.configurations;
using oculusit.sync.core.interfaces;
using oculusit.sync.core.services;

namespace oculusit.sync.core;

public static class CoreServiceExtensions
{
    public static IServiceCollection AddCoreServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FileSyncStateConfiguration>(
            configuration.GetSection(FileSyncStateConfiguration.SectionName));

        services.AddSingleton<ISyncStateService, FileSyncStateService>();

        return services;
    }
}
