using CorUI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CorUI;

public static class ServiceCollectionExtensions
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddCorUIWeb(this IServiceCollection services)
    {
        return services
            .AddScoped<IViewStorage, WebViewStorage>()
            .AddScoped<IWindowService, WebWindowService>();
    }
    
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddCorUINative<T>(this IServiceCollection services, BlazorWebViewOptions options) where T : class, ICorUIApplication
    {
        return services
            .AddSingleton<ICorUIApplication, T>()
            .AddSingleton(options);
    }
}