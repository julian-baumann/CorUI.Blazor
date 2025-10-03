using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using WebKit;

namespace CorUI.macOS;

public sealed class BlazorWebView(IServiceProvider serviceProvider, Window window) : NSViewController
{
    public event Action? Ready;
    private bool IsReady { get; set; }
    private WKWebView? _webView;
    private AppKitWebViewManager? _manager;
    private WKUserContentController? _userContentController;
    private ScriptMessageHandler? _scriptMessageHandler;
    private ScriptMessageHandler? _hostMessageHandler;
    private AppUrlSchemeHandlerWithManager? _schemeHandler;
    private PhysicalFileProvider? _physicalFileProvider;

    public override void LoadView()
    {
        var bounds = new CGRect(0, 0, window.Width, window.Height);
        View = new DraggableVisualEffectView(bounds, window)
        {
            AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable,
            BlendingMode = NSVisualEffectBlendingMode.BehindWindow,
            Material = NSVisualEffectMaterial.UnderWindowBackground,
        };
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();

        var options = serviceProvider.GetRequiredService<BlazorWebViewOptions>();

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
        
        var hostBridgeJs = """
            (() => {
               if (!window.host) { window.host = {}; }
               window.host.notify = function(m) {
                   try { window.webkit?.messageHandlers?.host?.postMessage(m); } catch {}
               };
            })();
            """;
        _userContentController!.AddUserScript(new WKUserScript(new NSString(hostBridgeJs), WKUserScriptInjectionTime.AtDocumentStart, true));
        _hostMessageHandler = new ScriptMessageHandler(message =>
        {
            if (string.Equals(message, "ready", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsReady)
                {
                    IsReady = true;
                    Ready?.Invoke();
                }
            }
        });
        _userContentController.AddScriptMessageHandler(_hostMessageHandler, "host");
        
        _scriptMessageHandler = new ScriptMessageHandler(msg => _manager?.ForwardScriptMessage(msg));
        _userContentController.AddScriptMessageHandler(_scriptMessageHandler, "webview");
        
        InjectHtmlClasses(
            "mac",
            "vibrancy",
            "window-buttons-left",
            window.MacWindowOptions.MacTrafficLightStyle == MacTrafficLightStyle.Expanded
                ? "toolbar-expanded"
                : "toolbar-compact"
        );

        _schemeHandler = new AppUrlSchemeHandlerWithManager(() => _manager);

        config.UserContentController = _userContentController;
        config.SetUrlSchemeHandler(_schemeHandler, "app");

        var webView = new DraggableWkWebView(View.Bounds, config)
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
            serviceProvider,
            fileProvider,
            options.RelativeHostPath,
            options.RootComponent,
            serviceProvider.GetService<ILogger<AppKitWebViewManager>>());

        _manager.Navigate(new Uri(AppKitWebViewManager.BaseUri, "/").ToString());
    }
    

    private void InjectHtmlClasses(params string?[] classes)
    {
        if (_userContentController is null)
        {
            return;
        }

        var filtered = classes.Where(c => !string.IsNullOrWhiteSpace(c)).ToArray();
        if (filtered.Length == 0)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(filtered);
        var js = $"(function(c){{var e=document.documentElement||document.getElementsByTagName('html')[0];if(!e){{return;}}for(var i=0;i<c.length;i++){{e.classList.add(c[i]);}}}})({payload});";

        _userContentController.AddUserScript(
            new WKUserScript(new NSString(js), WKUserScriptInjectionTime.AtDocumentStart, true));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_manager is not null)
            {
                if (_manager is IAsyncDisposable asyncDisposable)
                {
                    try
                    {
                        _ = asyncDisposable.DisposeAsync();
                    }
                    catch
                    {
                        // ignored
                    }
                }

                _manager = null;
            }

            // 2) Now it’s safe to remove JS handlers & user scripts
            if (_userContentController is { } controller)
            {
                if (_scriptMessageHandler is { } handler)
                {
                    controller.RemoveScriptMessageHandler("webview");
                    handler.Dispose();
                    _scriptMessageHandler = null;
                }

                if (_hostMessageHandler is { } hm)
                {
                    controller.RemoveScriptMessageHandler("host");
                    hm.Dispose();
                    _hostMessageHandler = null;
                }

                controller.RemoveAllUserScripts();
                controller.Dispose();
                _userContentController = null;
            }

            // 3) Scheme handler and providers
            if (_schemeHandler is { } schemeHandler)
            {
                schemeHandler.Dispose();
                _schemeHandler = null;
            }

            if (_physicalFileProvider is { } physicalProvider)
            {
                physicalProvider.Dispose();
                _physicalFileProvider = null;
            }

            // 4) Finally tear down the WebView
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

    private sealed class ScriptMessageHandler(Action<string> onMessage) : NSObject, IWKScriptMessageHandler
    {
        public void DidReceiveScriptMessage(WKUserContentController _, WKScriptMessage message)
        {
            onMessage(message.Body.ToString() ?? string.Empty);
        }
    }
}
