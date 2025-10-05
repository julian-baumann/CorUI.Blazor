using CorUI.Services;

namespace CorUI.macOS.Services;

public sealed class MacWindowService(IServiceProvider serviceProvider) : IWindowService
{
    public Task OpenWindow(Window window)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        NSApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            try
            {
                var controller = new BlazorWindowController(null, window, serviceProvider);
                controller.ShowWindow(null);
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        
        return tcs.Task;
    }

    public Task OpenDialog(Dialog window)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        NSApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            try
            {
                // Create a sheet-style NSWindow attached to the key window
                var parent = NSApplication.SharedApplication.KeyWindow;
                if (parent is null)
                {
                    _ = OpenWindow(new Window
                    {
                        ContentPath = window.ContentPath,
                        Title = window.Title,
                        Width = window.Width,
                        Height = window.Height,
                        EnableDrag = false
                    });
                    tcs.TrySetResult(true);
                    return;
                }

                var dialogWindow = new NSWindow(
                    new CGRect(0, 0, window.Width, window.Height),
                    NSWindowStyle.Titled,
                    NSBackingStore.Buffered,
                    deferCreation: true)
                {
                    Title = string.IsNullOrWhiteSpace(window.Title) ? string.Empty : window.Title
                };

                var vc = new BlazorWebView(serviceProvider, new Window
                {
                    ContentPath = window.ContentPath,
                    Title = window.Title,
                    Width = window.Width,
                    Height = window.Height,
                    EnableDrag = false
                });
                dialogWindow.ContentViewController = vc;

                void ShowSheet()
                {
                    NSObject? keyMonitor = null;
                    if (window.DismissWithEscape)
                    {
                        keyMonitor = NSEvent.AddLocalMonitorForEventsMatchingMask(NSEventMask.KeyDown, ev =>
                        {
                            // 53 is Escape key
                            if (ev.KeyCode == 53)
                            {
                                try { parent.EndSheet(dialogWindow); } catch { }
                                return null;
                            }
                            return ev;
                        });
                    }

                    parent.BeginSheet(dialogWindow, _ =>
                    {
                        if (keyMonitor is not null)
                        {
                            try { NSEvent.RemoveMonitor(keyMonitor); } catch { }
                            keyMonitor.Dispose();
                        }
                        try { dialogWindow.Dispose(); } catch { }
                        tcs.TrySetResult(true);
                    });
                }

                void OnReady()
                {
                    vc.Ready -= OnReady;
                    ShowSheet();
                }

                vc.Ready += OnReady;
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        
        return tcs.Task;
    }

}


