using Microsoft.Extensions.Hosting;
using System.Windows;

namespace CorUI.Wpf;

// ReSharper disable once InconsistentNaming
public sealed class WpfApplication : IHostedService
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    public WpfApplication(IHostApplicationLifetime lifetime, IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        // App run is triggered by TestApp.Wpf; no explicit Main loop here.
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Application.Current?.Dispatcher.Invoke(() => Application.Current?.Shutdown());
        return Task.CompletedTask;
    }
}


