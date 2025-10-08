using System.Drawing;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.AspNetCore.Components.WebView;
using System.Windows;

namespace CorUI.Wpf;

public partial class BlazorHostWindow
{
    public BlazorHostWindow()
    {
        InitializeComponent();
    }

    public void Initialize(IServiceProvider serviceProvider, BlazorWebViewOptions options, string startPath)
    {
        
        Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "0");
        WebView.WebView.DefaultBackgroundColor = Color.Transparent;
        WebView.HostPage = options.HostPath;
        WebView.Services = serviceProvider;
        WebView.RootComponents.Add(new RootComponent { Selector = "#app", ComponentType = options.RootComponent });
        WebView.StartPath = string.IsNullOrWhiteSpace(startPath) ? "/" : startPath;
    }
}


