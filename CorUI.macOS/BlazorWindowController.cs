namespace CorUI.macOS;

public sealed class BlazorWindowController : NSWindowController
{
    private readonly BlazorWebView _contentController;
    private readonly Action<BlazorWindowController>? _onClosed;
    private WindowDelegate? _windowDelegate;
    private NSToolbar? _toolbar;

    public BlazorWindowController(Action<BlazorWindowController>? onClosed) : base(CreateWindow())
    {
        _onClosed = onClosed;
        _contentController = new BlazorWebView();

        if (Window is null)
        {
            throw new InvalidOperationException("Unable to create application window.");
        }
        

        Window.ContentViewController = _contentController;
        Window.Title = NSProcessInfo.ProcessInfo.ProcessName;
        Window.Center();
        Window.TitleVisibility = NSWindowTitleVisibility.Hidden;
        Window.TitlebarAppearsTransparent = true;
        // Window.IsOpaque = false;
        // Window.Toolbar = new NSToolbar();
        // Window.ToolbarStyle = NSWindowToolbarStyle.Expanded;
        _toolbar = new NSToolbar();
        Window.Toolbar = _toolbar;

        _windowDelegate = new WindowDelegate(this);
        Window.Delegate = _windowDelegate;
    }
    
    private void UpdateToolbarVisibility(bool isFullScreen)
    {
        if (Window is null)
        {
            return;
        }
        if (isFullScreen)
        {
            Window.Toolbar = null;
        }
        else
        {
            Window.Toolbar = _toolbar ??= new NSToolbar();
        }
    }

    public override void ShowWindow(NSObject? sender)
    {
        base.ShowWindow(sender);
        Window?.MakeKeyAndOrderFront(sender);
    }

    private void HandleClosed()
    {
        _onClosed?.Invoke(this);
    }

    private static NSWindow CreateWindow() => new(
        new CGRect(0, 0, 800, 500),
        NSWindowStyle.Titled | NSWindowStyle.Closable | NSWindowStyle.Resizable | NSWindowStyle.Miniaturizable | NSWindowStyle.FullSizeContentView | NSWindowStyle.Titled,
        NSBackingStore.Buffered,
        deferCreation: false);

    private sealed class WindowDelegate : NSWindowDelegate
    {
        private readonly BlazorWindowController _owner;

        public WindowDelegate(BlazorWindowController owner)
        {
            _owner = owner;
        }

        public override void WillClose(NSNotification notification)
        {

            // if (_owner.Window is { } window)
            // {
            //     window.Delegate = null!;
            //     window.ContentViewController = null!;
            // }

            _owner._contentController.Dispose();
            _owner._windowDelegate = null;
            _owner.HandleClosed();
        }

        public override void WillEnterFullScreen(NSNotification notification)
        {
            _owner.UpdateToolbarVisibility(isFullScreen: true);
        }
        public override void DidExitFullScreen(NSNotification notification)
        {
            _owner.UpdateToolbarVisibility(isFullScreen: false);
        }
    }

}
