namespace CorUI.macOS;

public sealed class BlazorWindowController : NSWindowController
{
    private readonly BlazorWebView _contentController;
    private readonly Action<BlazorWindowController>? _onClosed;
    private readonly Window _config;
    private WindowDelegate? _windowDelegate;
    private NSToolbar? _toolbar;

    public BlazorWindowController(Action<BlazorWindowController>? onClosed, Window windowConfiguration, IServiceProvider serviceProvider) : base(CreateWindow(windowConfiguration))
    {
        _onClosed = onClosed;
        _config = windowConfiguration;
        _contentController = new BlazorWebView(serviceProvider, windowConfiguration);
        _contentController.Ready += () =>
        {
            Window.MakeKeyAndOrderFront(null);
        };

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

    private static NSWindow CreateWindow(Window windowConfiguration) => new(
        new CGRect(0, 0, windowConfiguration.Width, windowConfiguration.Height),
        NSWindowStyle.FullSizeContentView | NSWindowStyle.Titled,
        NSBackingStore.Buffered,
        deferCreation: true);

    private sealed class WindowDelegate(BlazorWindowController owner) : NSWindowDelegate
    {
        public override void WillClose(NSNotification notification)
        {
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