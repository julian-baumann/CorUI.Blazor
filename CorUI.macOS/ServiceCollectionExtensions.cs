using CorUI.macOS.Services;
using CorUI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CorUI.macOS;

public static class ServiceCollectionExtensions
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddMacOS(this IServiceCollection services)
    {
        return services
            .AddSingleton<IViewStorage, ViewStorage>()
            .AddHostedService<MacOSApplication>()
            .AddBlazorWebView();
    }
}