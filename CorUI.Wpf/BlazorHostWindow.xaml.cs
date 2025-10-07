using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.AspNetCore.Components.WebView;
using System.Windows;

namespace CorUI.Wpf;

public partial class BlazorHostWindow : Window
{
    public BlazorHostWindow()
    {
        InitializeComponent();
    }

    public void Initialize(IServiceProvider serviceProvider, BlazorWebViewOptions options, string startPath)
    {
        WebView.HostPage = options.RelativeHostPath;
        WebView.Services = serviceProvider;
        WebView.RootComponents.Add(new RootComponent { Selector = "#app", ComponentType = options.RootComponent });
        WebView.StartPath = string.IsNullOrWhiteSpace(startPath) ? "/" : startPath;
    }
}


