using Microsoft.Extensions.DependencyInjection;

namespace CorUI;

public static class ServiceCollectionExtensions
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddCorUI<T>(this IServiceCollection services, BlazorWebViewOptions options) where T : class, ICorUIApplication
    {
        return services
            .AddSingleton<ICorUIApplication, T>()
            .AddSingleton(options);
    }
}