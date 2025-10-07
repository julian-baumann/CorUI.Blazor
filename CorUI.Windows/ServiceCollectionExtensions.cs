using CorUI.Services;
using CorUI.Windows.Services;
using Microsoft.AspNetCore.Components.WebView.WinUI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CorUI.Windows;

public static class ServiceCollectionExtensions
{
    // ReSharper disable once InconsistentNaming
    public static IServiceCollection AddWindows(this IServiceCollection services)
    {
        services
            .AddBlazorWebView()
            .TryAddSingleton(new BlazorWebViewDeveloperTools { Enabled = false });

        services.AddBlazorWebViewDeveloperTools();
        services.TryAddSingleton(_ => new WinUIBlazorMarkerService());

        services.TryAddSingleton<IViewStorage, ViewStorage>();
        services.AddScoped<WindowsWindowService>();
        services.AddScoped<IWindowService>(sp => sp.GetRequiredService<WindowsWindowService>());
        services.AddScoped<IDialogControlService>(sp => sp.GetRequiredService<WindowsWindowService>());

        return services;
        
        //.AddSingleton<IViewStorage, ViewStorage>()
        //.AddHostedService<MacOSApplication>()
        //.AddBlazorWebView()
        //.AddScoped<MacWindowService>()
        //.AddScoped<IWindowService>(sp => sp.GetRequiredService<MacWindowService>())
        //.AddScoped<IDialogControlService>(sp => sp.GetRequiredService<MacWindowService>());
    }
}