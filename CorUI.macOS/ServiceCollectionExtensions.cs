using Microsoft.Extensions.DependencyInjection;

namespace CorUI.macOS;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMacOS(this IServiceCollection services, BlazorWebViewOptions options)
    {
        return services
            .AddBlazorWebView()
            .AddSingleton(options);
    }
}