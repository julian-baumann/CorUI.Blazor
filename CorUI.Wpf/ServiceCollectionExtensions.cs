using CorUI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CorUI.Wpf;

public static class ServiceCollectionExtensions
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddWpf(this IServiceCollection services)
    {
        return services
            .AddSingleton<IViewStorage, ViewStorage>()
            .AddHostedService<WpfApplication>()
            .AddBlazorWebView()
            .AddScoped<WpfWindowService>()
            .AddScoped<IWindowService>(sp => sp.GetRequiredService<WpfWindowService>())
            .AddScoped<IDialogControlService>(sp => sp.GetRequiredService<WpfWindowService>());
    }
}


