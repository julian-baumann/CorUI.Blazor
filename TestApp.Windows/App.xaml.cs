using CorUI.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        var builder = Host.CreateApplicationBuilder();

        //builder.Services.AddSingleton<IApp>(app);
        builder.Services.AddWindows();
        builder.Services.AddSingleton<MainWindow>();

        return builder.Build();
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
