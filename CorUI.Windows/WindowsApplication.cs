//using Microsoft.Extensions.Hosting;
//using Microsoft.UI.Xaml;
//using System;
//using System.Collections.Immutable;
//using System.Threading;
//using System.Threading.Tasks;

//namespace CorUI.Windows;

//internal partial class WindowsApplication : Application, IHostedService
//{
//    public WindowsApplication(IHostApplicationLifetime lifetime, IServiceProvider serviceProvider)
//    {
//    }

//    public App(string[] args)
//    {
//        this.arguments = ImmutableArray.Create(args);

//        InitializeComponent();
//    }

//    protected override void OnLaunched(LaunchActivatedEventArgs args)
//    {
//        // The host should already be created by this time.
//        if (host == null)
//        {
//            throw new InvalidOperationException();
//        }

//        base.OnLaunched(args);

//        mainWindow = host.Services.GetRequiredService<MainWindow>();
//        mainWindow.Closed += OnMainWindowClosed;
//        mainWindow.AppWindow.Show(true);
//    }

//    private void OnMainWindowClosed(object sender, WindowEventArgs args)
//    {
//        Exit();
//    }


//    public Task StartAsync(CancellationToken cancellationToken)
//    {
//        return Task.CompletedTask;
//    }

//    public Task StopAsync(CancellationToken cancellationToken)
//    {
//        return Task.CompletedTask;
//    }
//}
