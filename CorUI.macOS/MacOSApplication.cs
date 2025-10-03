using Microsoft.Extensions.Hosting;

namespace CorUI.macOS;

// ReSharper disable once InconsistentNaming
public class MacOSApplication : IHostedService
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    public MacOSApplication(IHostApplicationLifetime lifetime, IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;

        NSApplication.Init();
        NSApplication.SharedApplication.Delegate = new AppDelegate(serviceProvider);
        NSApplication.Main(["-NSQuitAlwaysKeepsWindows", "NO"]);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}