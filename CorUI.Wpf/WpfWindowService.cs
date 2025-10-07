using CorUI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.AspNetCore.Components.WebView;

namespace CorUI.Wpf;

public sealed class WpfWindowService(IServiceProvider serviceProvider) : IWindowService, IDialogControlService
{
    private Window? _activeDialogWindow;

    public Task OpenWindow(CorUI.Window window)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                var host = CreateBlazorHostWindow(window);
                host.Owner = null;
                host.Show();
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    public Task OpenDialog(CorUI.Dialog dialog)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                var owner = Application.Current?.Windows.Cast<Window?>().FirstOrDefault(w => w is not null && w.IsActive);
                var host = CreateBlazorHostWindow(new CorUI.Window
                {
                    ContentPath = dialog.ContentPath,
                    Title = dialog.Title,
                    Width = dialog.Width,
                    Height = dialog.Height,
                    EnableDrag = false
                });
                if (owner is not null)
                {
                    host.Owner = owner;
                }
                _activeDialogWindow = host;
                host.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                host.ShowDialog();
                _activeDialogWindow = null;
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    public Task CloseActiveDialog()
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                _activeDialogWindow?.Close();
                _activeDialogWindow = null;
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }

    private Window CreateBlazorHostWindow(CorUI.Window window)
    {
        var options = serviceProvider.GetRequiredService<BlazorWebViewOptions>();

        var w = new Window
        {
            Title = string.IsNullOrWhiteSpace(window.Title) ? string.Empty : window.Title,
            Width = window.Width,
            Height = window.Height,
            WindowStyle = WindowStyle.SingleBorderWindow,
            ResizeMode = ResizeMode.CanResize
        };

        var bvw = new BlazorWebView
        {
            HostPage = options.RelativeHostPath
        };
        bvw.Services = serviceProvider;
        bvw.RootComponents.Add(new RootComponent { Selector = "#app", ComponentType = options.RootComponent });

        // Navigate to route when ready
        var targetPath = string.IsNullOrWhiteSpace(window.ContentPath) ? "/" : window.ContentPath;
        bvw.Loaded += (_, _) =>
        {
            try
            {
                var nav = serviceProvider.GetService<Microsoft.AspNetCore.Components.NavigationManager>();
                if (nav is not null)
                {
                    // Workaround: NavigationManager resolves only inside components; instead push base relative URI into WebView start path.
                }
            }
            catch
            {
            }
        };

        // Use query string navigation via baseURI
        bvw.StartPath = targetPath;

        w.Content = bvw;
        return w;
    }
}


