using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.IO;
using WebKit;

namespace CorUI.macOS;

public sealed class BlazorWebView : NSViewController
{
    private WKWebView? _webView;
    private AppKitWebViewManager? _manager;
    private WKUserContentController? _userContentController;
    private ScriptMessageHandler? _scriptMessageHandler;
    private AppUrlSchemeHandlerWithManager? _schemeHandler;
    private PhysicalFileProvider? _physicalFileProvider;

    public override void LoadView()
    {
        var bounds = new CoreGraphics.CGRect(0, 0, 1200, 800);
        View = new DraggableVisualEffectView(bounds)
        {
            AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable,
            BlendingMode = NSVisualEffectBlendingMode.BehindWindow,
            Material = NSVisualEffectMaterial.UnderWindowBackground,
        };
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        var services = MacOSApplication.ServiceProvider;
        var options = services.GetRequiredService<BlazorWebViewOptions>();

        var config = new WKWebViewConfiguration();
        _userContentController = new WKUserContentController();

        var bridgeJs = """
            (() => {
                const h = 'webview';
                if (!window.external) { window.external = {}; }
                if (!window.__receiveMessageCallbacks) { window.__receiveMessageCallbacks = []; }
                if (!window.external.sendMessage) { window.external.sendMessage = m => window.webkit?.messageHandlers?.[h]?.postMessage(m); }
                if (!window.external.receiveMessage) { window.external.receiveMessage = cb => window.__receiveMessageCallbacks.push(cb); }
                window.__dispatchMessageCallback = m => { for (const cb of window.__receiveMessageCallbacks) { try { cb(m); } catch { } } };
            })();
            """;

        _userContentController.AddUserScript(new WKUserScript(new NSString(bridgeJs), WKUserScriptInjectionTime.AtDocumentStart, true));
        _scriptMessageHandler = new ScriptMessageHandler(msg => _manager?.ForwardScriptMessage(msg));
        _userContentController.AddScriptMessageHandler(_scriptMessageHandler, "webview");

        _schemeHandler = new AppUrlSchemeHandlerWithManager(() => _manager);

        config.UserContentController = _userContentController;
        config.SetUrlSchemeHandler(_schemeHandler, "app");

        var webView = new DraggableWKWebView(View.Bounds, config)
        {
            AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable,
            UnderPageBackgroundColor = NSColor.Clear
        };
        if (View is DraggableVisualEffectView dragHost)
        {
            webView.DragRegionHeight = dragHost.DragRegionHeight;
        }
        _webView = webView;
        if (OperatingSystem.IsMacOSVersionAtLeast(13, 3))
        {
            _webView.Inspectable = true;
        }
        _webView.SetValueForKey(NSObject.FromObject(false), new NSString("drawsBackground"));
        
        if (View is NSVisualEffectView effectView)
        {
            effectView.AddSubview(_webView);
        }
        else
        {
            View.AddSubview(_webView);
        }

        var webRootPath = Path.Combine(NSBundle.MainBundle.ResourcePath!, "wwwroot");
        _physicalFileProvider = new PhysicalFileProvider(webRootPath);
        var fileProvider = new CompositeFileProvider(
            _physicalFileProvider,
            new ManifestEmbeddedFileProvider(typeof(WebViewManager).Assembly));

        _manager = new AppKitWebViewManager(
            _webView,
            services,
            fileProvider,
            options.RelativeHostPath,
            options.RootComponent,
            services.GetService<ILogger<AppKitWebViewManager>>());

        _manager.Navigate(new Uri(AppKitWebViewManager.BaseUri, "/").ToString());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_physicalFileProvider is { } physicalProvider)
            {
                physicalProvider.Dispose();
                _physicalFileProvider = null;
            }
            if (_userContentController is { } controller)
            {
                if (_scriptMessageHandler is { } handler)
                {
                    controller.RemoveScriptMessageHandler("webview");
                    handler.Dispose();
                    _scriptMessageHandler = null;
                }

                controller.RemoveAllUserScripts();
                controller.Dispose();
                _userContentController = null;
            }

            if (_schemeHandler is { } schemeHandler)
            {
                schemeHandler.Dispose();
                _schemeHandler = null;
            }

            if (_manager is not null)
            {
                switch (_manager)
                {
                    case IAsyncDisposable asyncDisposable:
                        asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                        break;
                }

                _manager = null;
            }

            if (_webView is { } webView)
            {
                webView.StopLoading();
                webView.NavigationDelegate = null!;
                webView.UIDelegate = null!;
                webView.RemoveFromSuperview();
                webView.Dispose();
                _webView = null;
            }
        }

        base.Dispose(disposing);
    }

    private sealed class ScriptMessageHandler : NSObject, IWKScriptMessageHandler
    {
        private readonly Action<string> _onMessage;
        public ScriptMessageHandler(Action<string> onMessage) { _onMessage = onMessage; }
        public void DidReceiveScriptMessage(WKUserContentController _, WKScriptMessage message)
        {
            _onMessage(message.Body?.ToString() ?? string.Empty);
        }
    }
}
