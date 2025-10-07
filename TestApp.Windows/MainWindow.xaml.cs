using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace TestApp.Windows
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "00000000");
            BlazorWebView.Services = serviceProvider;

            BlazorWebView.BlazorWebViewInitializing += BlazorWebView_BlazorWebViewInitializing;
        }

        private void BlazorWebView_BlazorWebViewInitializing(object? sender, Microsoft.AspNetCore.Components.WebView.BlazorWebViewInitializingEventArgs e)
        {
            BlazorWebView.WebView.DefaultBackgroundColor = Color.FromArgb(0, 0, 0, 0);
        }
    }
}
