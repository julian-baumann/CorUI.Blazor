using CorUI.Services;

namespace CorUI.macOS.Services;

public sealed class MacWindowService(IServiceProvider serviceProvider) : IWindowService, IDialogControlService
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

    private NSWindow? _activeSheet;
    private int _dialogVersion;

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

                var versionAtStart = ++_dialogVersion;
                bool sheetStarted = false;
                NSTimer? fallbackTimer = null;
                void ShowSheet()
                {
                    if (sheetStarted || _dialogVersion != versionAtStart)
                    {
                        return;
                    }
                    if (parent is null || parent.Handle == IntPtr.Zero || !parent.IsVisible)
                    {
                        tcs.TrySetResult(true);
                        return;
                    }
                    sheetStarted = true;
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
                        try { fallbackTimer?.Invalidate(); } catch { }
                        _activeSheet = null;
                        _dialogVersion++; // prevent any pending re-open
                        sheetStarted = false;
                        tcs.TrySetResult(true);
                    });
                    _activeSheet = dialogWindow;
                }

                void OnReady()
                {
                    vc.Ready -= OnReady;
                    ShowSheet();
                }

                vc.Ready += OnReady;
                // Fallback after 3s to ensure sheet shows even if Ready not fired yet
                fallbackTimer = NSTimer.CreateScheduledTimer(TimeSpan.FromSeconds(1.5), _ =>
                {
                    if (_dialogVersion == versionAtStart)
                    {
                        ShowSheet();
                    }
                });
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
        NSApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            try
            {
                _dialogVersion++;
                var parent = NSApplication.SharedApplication.KeyWindow;
                var sheet = _activeSheet ?? parent?.AttachedSheet ?? parent?.SheetParent?.AttachedSheet;
                if (parent is not null && sheet is not null && parent.Handle != IntPtr.Zero && sheet.Handle != IntPtr.Zero)
                {
                    try { parent.EndSheet(sheet); } catch { }
                    try { sheet.OrderOut(null); } catch { }
                    _activeSheet = null;
                }
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }

}


