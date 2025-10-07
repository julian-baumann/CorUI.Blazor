using System;
using System.Threading.Tasks;
using CorUI.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace CorUI.Windows.Services;

public sealed class WindowsWindowService(IServiceProvider serviceProvider, BlazorWebViewOptions options) : IWindowService, IDialogControlService
{
    private Microsoft.UI.Xaml.Window? _activeDialogWindow;

    public Task OpenWindow(Window window)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            var win = new Microsoft.UI.Xaml.Window
            {
                Title = string.IsNullOrWhiteSpace(window.Title) ? string.Empty : window.Title
            };

            var grid = new Grid();
            var blazor = CreateBlazorWebView(window.ContentPath);
            grid.Children.Add(blazor);
            win.Content = grid;

            try
            {
                win.AppWindow.Resize(new SizeInt32(window.Width, window.Height));
            }
            catch
            {
                // Ignore if AppWindow APIs are not available
            }

            win.Activate();
            tcs.TrySetResult(true);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }

    public Task OpenDialog(Dialog window)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            var dlg = new Microsoft.UI.Xaml.Window
            {
                Title = string.IsNullOrWhiteSpace(window.Title) ? string.Empty : window.Title
            };

            var grid = new Grid();
            var blazor = CreateBlazorWebView(window.ContentPath);
            grid.Children.Add(blazor);
            dlg.Content = grid;

            try
            {
                dlg.AppWindow.Resize(new SizeInt32(window.Width, window.Height));
            }
            catch
            {
            }

            _activeDialogWindow = dlg;
            dlg.Closed += (_, _) =>
            {
                if (ReferenceEquals(_activeDialogWindow, dlg))
                {
                    _activeDialogWindow = null;
                }
                tcs.TrySetResult(true);
            };

            dlg.Activate();
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }

    public Task CloseActiveDialog()
    {
        if (_activeDialogWindow is not null)
        {
            try
            {
                _activeDialogWindow.Close();
            }
            catch
            {
            }
            finally
            {
                _activeDialogWindow = null;
            }
        }

        return Task.CompletedTask;
    }

    private BlazorWebView CreateBlazorWebView(string? contentPath)
    {
        var bwv = new BlazorWebView
        {
            Services = serviceProvider,
            HostPage = options.HostPath,
            StartPath = string.IsNullOrWhiteSpace(contentPath) ? "/" : contentPath
        };

        bwv.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = options.RootComponent
        });

        return bwv;
    }
}


