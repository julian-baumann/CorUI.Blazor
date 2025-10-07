using CorUI;
using CorUI.Windows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Immutable;
using System.Threading;

namespace TestApp.Windows;

public partial class App : Application
{
    private static IHost? host;

    [STAThread]
    public static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        try
        {
            Start(_ =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);

                var app = new App(args);
                app.UnhandledException += (_, _) => StopHost();

                host = CreateHost();
            });
        }
        finally
        {
            StopHost();
        }
    }

    private static IHost CreateHost()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddLogging();

        // Windows host uses BlazorWebView; do not register web/server services here
        builder.Services.AddWindows();
        builder.Services.AddCorUINative<TestApp.Windows.AppHost>(
            new BlazorWebViewOptions
            {
                RootComponent = typeof(TestApp.App)
            }
        );
        builder.Services.AddSingleton<MainWindow>();

        var app = builder.Build();

        return app;
    }

    private static void StopHost()
    {
        host?.Dispose();
    }

    private readonly ImmutableArray<string> arguments;
    private MainWindow? mainWindow;

    public App(string[] args)
    {
        this.arguments = ImmutableArray.Create(args);

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // The host should already be created by this time.
        if (host == null)
        {
            throw new InvalidOperationException();
        }

        base.OnLaunched(args);

        mainWindow = host.Services.GetRequiredService<MainWindow>();
        mainWindow.Closed += OnMainWindowClosed;
        mainWindow.AppWindow.Show(true);
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        Exit();
    }
}
