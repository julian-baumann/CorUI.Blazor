namespace CorUI.macOS;

public sealed class BlazorWindowController : NSWindowController
{
    private readonly BlazorWebView _contentController;
    private readonly Action<BlazorWindowController>? _onClosed;
    private readonly Window _config;
    private WindowDelegate? _windowDelegate;
    private NSToolbar? _toolbar;
    private NSTimer? _showFallbackTimer;
    private bool _isClosed;

    public BlazorWindowController(Action<BlazorWindowController>? onClosed, Window windowConfiguration, IServiceProvider serviceProvider) : base(CreateWindow(windowConfiguration))
    {
        _onClosed = onClosed;
        _config = windowConfiguration;
        _contentController = new BlazorWebView(serviceProvider, windowConfiguration);
        _contentController.Ready += OnContentReady;
        // Fallback: show the window after 3 seconds if Ready hasn't fired yet
        _showFallbackTimer = NSTimer.CreateScheduledTimer(TimeSpan.FromSeconds(3), _ =>
        {
            if (_isClosed)
            {
                return;
            }
            if (!Window.IsKeyWindow || !Window.IsVisible)
            {
                ShowAndFocusWindow();
            }
        });

        if (Window is null)
        {
            throw new InvalidOperationException("Unable to create application window.");
        }

        _toolbar = new NSToolbar();
        ApplyWindowConfiguration();

        Window.ContentViewController = _contentController;
        Window.Center();

        _windowDelegate = new WindowDelegate(this);
        Window.Delegate = _windowDelegate;
    }

    private void OnContentReady()
    {
        if (_isClosed)
        {
            return;
        }
        ShowAndFocusWindow();
    }

    public override void ShowWindow(NSObject? sender)
    {
        // base.ShowWindow(sender);
        // Window.MakeKeyAndOrderFront(sender);

        if (_config.IsFullScreen)
        {
            Window.ToggleFullScreen(sender);
        }
    }

    private void ApplyWindowConfiguration()
    {
        // Title
        Window.Title = string.IsNullOrWhiteSpace(_config.Title)
            ? NSProcessInfo.ProcessInfo.ProcessName
            : _config.Title;

        // Style masks
        Window.StyleMask |= NSWindowStyle.FullSizeContentView | NSWindowStyle.Titled;

        if (_config.CanClose)
        {
            Window.StyleMask |= NSWindowStyle.Closable;
        }
        if (_config is { CanMaximize: true, CanResize: true })
        {
            Window.StyleMask |= NSWindowStyle.Resizable;
        }
        if (_config.CanMinimize)
        {
            Window.StyleMask |= NSWindowStyle.Miniaturizable;
        }

        // Titlebar look + toolbar style (maps your MacTrafficLightStyle)
        Window.TitleVisibility = NSWindowTitleVisibility.Hidden;
        Window.TitlebarAppearsTransparent = true;

        if (_config.MacWindowOptions.MacTrafficLightStyle is MacTrafficLightStyle.Expanded)
        {
            Window.Toolbar = _toolbar;
        }

        // Honor traffic-light visibility (close/minimize/zoom buttons)
        var closeBtn = Window.StandardWindowButton(NSWindowButton.CloseButton);
        closeBtn.Hidden = !_config.ShowCloseButton;
        closeBtn.Enabled = _config.CanClose;

        var miniBtn = Window.StandardWindowButton(NSWindowButton.MiniaturizeButton);
        miniBtn.Hidden = !_config.ShowMinimizeButton;
        miniBtn.Enabled = _config.CanMinimize;

        var zoomBtn = Window.StandardWindowButton(NSWindowButton.ZoomButton);
        zoomBtn.Hidden = !_config.ShowMaximizeButton;
        // Zoom (maximize) generally requires resizable; enable matches config intent.
        zoomBtn.Enabled = _config.CanMaximize && _config.CanResize;
    }

    private void UpdateToolbarVisibility(bool isFullScreen)
    {
        if (isFullScreen)
        {
            Window.Toolbar = null;
        }
        else if (_config.MacWindowOptions.MacTrafficLightStyle is MacTrafficLightStyle.Expanded)
        {
            Window.Toolbar = _toolbar ??= new NSToolbar();
        }
    }

    private void HandleClosed()
    {
        _onClosed?.Invoke(this);
    }

    private void ShowAndFocusWindow()
    {
        if (_isClosed)
        {
            return;
        }
        NSApplication.SharedApplication.InvokeOnMainThread(() =>
        {
            try
            {
                // Activate app if supported, otherwise just bring window to front
                try { NSApplication.SharedApplication.ActivateIgnoringOtherApps(true); } catch { }
                if (Window is null || Window.Handle == IntPtr.Zero)
                {
                    return;
                }
                Window.OrderFrontRegardless();
                if (!Window.IsKeyWindow)
                {
                    Window.MakeKeyWindow();
                }
                if (Window.ContentView is { } cv)
                {
                    Window.MakeFirstResponder(cv);
                }
            }
            catch { }
        });
    }

    private static NSWindow CreateWindow(Window windowConfiguration) => new(
        new CGRect(0, 0, windowConfiguration.Width, windowConfiguration.Height),
        NSWindowStyle.FullSizeContentView | NSWindowStyle.Titled,
        NSBackingStore.Buffered,
        deferCreation: true);

    private sealed class WindowDelegate(BlazorWindowController owner) : NSWindowDelegate
    {
        public override void WillClose(NSNotification notification)
        {
            owner._isClosed = true;
            try
            {
                if (owner._showFallbackTimer is not null)
                {
                    owner._showFallbackTimer.Invalidate();
                    owner._showFallbackTimer = null;
                }
            }
            catch { }
            try { owner._contentController.Ready -= owner.OnContentReady; } catch { }
            owner._contentController.Dispose();
            owner._windowDelegate = null;
            owner.HandleClosed();
        }

        public override void WillEnterFullScreen(NSNotification notification)
        {
            owner.UpdateToolbarVisibility(isFullScreen: true);
        }

        public override void DidExitFullScreen(NSNotification notification)
        {
            owner.UpdateToolbarVisibility(isFullScreen: false);
        }
    }
}