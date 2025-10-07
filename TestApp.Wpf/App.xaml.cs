using CorUI;
using CorUI.Wpf;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;

namespace TestApp.Wpf;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = Host.CreateDefaultBuilder();
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });

        builder.ConfigureServices(services =>
        {
            services
                .AddCorUINative<TestAppApp>(
                    new BlazorWebViewOptions
                    {
                        RootComponent = typeof(TestApp.App)
                    }
                )
                .AddWpf()
                .AddRazorComponents();
        });

        _host = builder.Build();

        _ = _host.StartAsync();

        var corApp = _host.Services.GetRequiredService<ICorUIApplication>();
        var windowService = _host.Services.GetRequiredService<CorUI.Services.IWindowService>();
        _ = windowService.OpenWindow(corApp.StartWindow);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}

public sealed class TestAppApp : ICorUIApplication
{
    public CorUI.Window StartWindow => new()
    {
        ContentPath = "/",
        Title = "CorUI WPF",
        Width = 1024,
        Height = 768
    };
}


