using System;
using System.Threading.Tasks;
using CorUI.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using System.Runtime.InteropServices;
using WinRT.Interop;
using Windows.UI;
using Microsoft.UI.Dispatching;

namespace CorUI.Windows.Services;

public sealed class WindowsWindowService(IServiceProvider serviceProvider, BlazorWebViewOptions options) : IWindowService, IDialogControlService
{
    private Microsoft.UI.Xaml.Window? _activeDialogWindow;
    private AppWindow? _dialogOwner;
    private TypedEventHandler<AppWindow, AppWindowChangedEventArgs>? _ownerChangedHandler;
    private TypedEventHandler<AppWindow, AppWindowChangedEventArgs>? _dialogChangedHandler;
    private bool _suppressDialogRecenter;
    private AppWindow? _lastActiveAppWindow;
    private IntPtr _ownerHwnd;
    private SUBCLASSPROC? _ownerSubclassProc;
    private UIntPtr _ownerSubclassId = (UIntPtr)1;
    private GCHandle _ownerSubclassHandle;
    private bool _ownerSubclassHandleAllocated;
    private IntPtr _dialogHwnd;
    private SUBCLASSPROC? _dialogSubclassProc;
    private UIntPtr _dialogSubclassId = (UIntPtr)2;
    private GCHandle _dialogSubclassHandle;
    private bool _dialogSubclassHandleAllocated;
    private bool _dismissWithEscapeForCurrentDialog;

    public Task OpenWindow(Window window)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            var win = new Microsoft.UI.Xaml.Window
            {
                Title = string.IsNullOrWhiteSpace(window.Title) ? string.Empty : window.Title
            };

            var grid = new Grid();
            var blazor = CreateBlazorWebView(window.ContentPath);
            grid.Children.Add(blazor);
            win.Content = grid;

            try
            {
                win.AppWindow.Resize(new SizeInt32(window.Width, window.Height));
            }
            catch
            {
                // Ignore if AppWindow APIs are not available
            }

            win.Activate();
            try
            {
                _lastActiveAppWindow = win.AppWindow;
                win.Activated += (_, e) =>
                {
                    if (e.WindowActivationState != WindowActivationState.Deactivated)
                    {
                        _lastActiveAppWindow = win.AppWindow;
                    }
                };
            }
            catch
            {
            }
            tcs.TrySetResult(true);
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }

    public Task OpenDialog(Dialog window)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            var dlg = new Microsoft.UI.Xaml.Window
            {
            };

            var grid = new Grid();
            //grid.Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0));
            var blazor = CreateBlazorWebView(window.ContentPath);
            //var chrome = new Border
            //{
            //    CornerRadius = new CornerRadius(20),
            //    Background = new SolidColorBrush(global::Windows.UI.Color.FromArgb(0, 0, 0, 0)),
            //    BorderThickness = new Thickness(0)
            //};
            //chrome.Child = blazor;
            grid.Children.Add(blazor);
            dlg.Content = grid;

            try
            {
                var appWindow = dlg.AppWindow;

                // Make dialog borderless and without window buttons; non-resizable; similar to macOS sheet
                if (appWindow.Presenter is OverlappedPresenter presenter)
                {
                    presenter.IsResizable = false;
                    presenter.IsMinimizable = false;
                    presenter.IsMaximizable = false;
                    presenter.SetBorderAndTitleBar(false, false);
                }

                // Apply DWM visual styles (larger rounded corners, no visible border)
                TryApplyDialogWindowStyling(dlg);
                bool appliedOnActivated = false;
                dlg.Activated += (_, __) =>
                {
                    if (!appliedOnActivated)
                    {
                        appliedOnActivated = true;
                        TryApplyDialogWindowStyling(dlg);
                    }
                };

                // Apply size respecting min/max constraints
                var width = window.Width;
                var height = window.Height;
                if (window.MinWidth > 0) width = Math.Max(width, window.MinWidth);
                if (window.MinHeight > 0) height = Math.Max(height, window.MinHeight);
                if (window.MaxWidth.HasValue) width = Math.Min(width, window.MaxWidth.Value);
                if (window.MaxHeight.HasValue) height = Math.Min(height, window.MaxHeight.Value);
                appWindow.Resize(new SizeInt32(width, height));

                // Determine parent to center over
                _dialogOwner = _lastActiveAppWindow ?? GetAppWindowFromCurrentWindow() ?? TryGetActiveAppWindowOfCurrentProcess();
                TrySetOwnedWindow(dlg, _dialogOwner, appWindow);
                CenterDialog(appWindow, _dialogOwner);

                // Keep dialog centered relative to parent when parent moves/resizes
                if (_dialogOwner is not null)
                {
                    _ownerChangedHandler = (sender, args) =>
                    {
                        if (args.DidPositionChange || args.DidSizeChange)
                        {
                            CenterDialog(appWindow, _dialogOwner);
                        }
                    };
                    _dialogOwner.Changed += _ownerChangedHandler;
                }

                // Prevent independent movement: recenter when dialog position changes
                _dialogChangedHandler = (sender, args) =>
                {
                    if (_suppressDialogRecenter)
                    {
                        return;
                    }
                    if (args.DidPositionChange)
                    {
                        _suppressDialogRecenter = true;
                        try
                        {
                            CenterDialog(appWindow, _dialogOwner);
                        }
                        finally
                        {
                            _suppressDialogRecenter = false;
                        }
                    }
                };
                appWindow.Changed += _dialogChangedHandler;

                // ESC to dismiss if enabled
                _dismissWithEscapeForCurrentDialog = window.DismissWithEscape;
                _dialogHwnd = WindowNative.GetWindowHandle(dlg);
                if (_dialogHwnd != IntPtr.Zero)
                {
                    _dialogSubclassProc = DialogWndProc;
                    _dialogSubclassHandle = GCHandle.Alloc(this);
                    _dialogSubclassHandleAllocated = true;
                    SetWindowSubclass(_dialogHwnd, _dialogSubclassProc, _dialogSubclassId, IntPtr.Zero);
                }

                // Also hook WebView2 accelerator keys so ESC works when focus is inside the web content
                blazor.BlazorWebViewInitialized += (_, __) =>
                {
                    try
                    {
                        var core = blazor.WebView?.CoreWebView2;
                        if (core is null)
                        {
                            return;
                        }
                        // Fallback: intercept KeyDown routed event on WebView itself
                        blazor.WebView.KeyDown += (s, e) =>
                        {
                            try
                            {
                                if (!_dismissWithEscapeForCurrentDialog)
                                {
                                    return;
                                }
                                if (e.Key == global::Windows.System.VirtualKey.Escape)
                                {
                                    e.Handled = true;
                                    _ = CloseActiveDialog();
                                }
                            }
                            catch
                            {
                            }
                        };
                    }
                    catch
                    {
                    }
                };
            }
            catch
            {
            }

            _activeDialogWindow = dlg;
            dlg.Closed += (_, _) =>
            {
                if (ReferenceEquals(_activeDialogWindow, dlg))
                {
                    _activeDialogWindow = null;
                }
                // Unsubscribe
                var appWindow = dlg.AppWindow;
                if (_dialogOwner is not null && _ownerChangedHandler is not null)
                {
                    _dialogOwner.Changed -= _ownerChangedHandler;
                }
                if (_dialogChangedHandler is not null)
                {
                    appWindow.Changed -= _dialogChangedHandler;
                }
                if (_ownerHwnd != IntPtr.Zero && _ownerSubclassProc is not null)
                {
                    RemoveWindowSubclass(_ownerHwnd, _ownerSubclassProc, _ownerSubclassId);
                }
                if (_dialogHwnd != IntPtr.Zero && _dialogSubclassProc is not null)
                {
                    RemoveWindowSubclass(_dialogHwnd, _dialogSubclassProc, _dialogSubclassId);
                }
                if (_ownerSubclassHandleAllocated)
                {
                    try
                    {
                        _ownerSubclassHandle.Free();
                    }
                    catch
                    {
                    }
                    _ownerSubclassHandleAllocated = false;
                }
                if (_dialogSubclassHandleAllocated)
                {
                    try
                    {
                        _dialogSubclassHandle.Free();
                    }
                    catch
                    {
                    }
                    _dialogSubclassHandleAllocated = false;
                }
                _ownerHwnd = IntPtr.Zero;
                _dialogHwnd = IntPtr.Zero;
                _dialogOwner = null;
                _ownerChangedHandler = null;
                _dialogChangedHandler = null;
                tcs.TrySetResult(true);
            };

            dlg.Activate();
        }
        catch (Exception ex)
        {
            tcs.TrySetException(ex);
        }

        return tcs.Task;
    }

    public Task CloseActiveDialog()
    {
        if (_activeDialogWindow is not null)
        {
            try
            {
                _activeDialogWindow.Close();
            }
            catch
            {
            }
            finally
            {
                _activeDialogWindow = null;
            }
        }

        return Task.CompletedTask;
    }

    private BlazorWebView CreateBlazorWebView(string? contentPath)
    {
        var bwv = new BlazorWebView
        {
            Services = serviceProvider,
            HostPage = options.HostPath,
            StartPath = string.IsNullOrWhiteSpace(contentPath) ? "/" : contentPath
        };

        bwv.RootComponents.Add(new RootComponent
        {
            Selector = "#app",
            ComponentType = options.RootComponent
        });

        try
        {
            Environment.SetEnvironmentVariable("WEBVIEW2_DEFAULT_BACKGROUND_COLOR", "00000000");
            bwv.BlazorWebViewInitializing += (_, e) =>
            {
                try
                {
                    bwv.WebView.DefaultBackgroundColor = global::Windows.UI.Color.FromArgb(0, 0, 0, 0);
                }
                catch
                {
                }
            };
        }
        catch
        {
        }

        return bwv;
    }

    private void TryApplyDialogWindowStyling(Microsoft.UI.Xaml.Window dialog)
    {
        try
        {
            var hwnd = WindowNative.GetWindowHandle(dialog);
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            SetWindowLongPtr(hwnd, GWL_STYLE, (IntPtr)(GetWindowLong(hwnd, GWL_STYLE) & ~(WS_DLGFRAME)));

            var pref = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND; // larger radius
            DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, ref pref, Marshal.SizeOf<DWM_WINDOW_CORNER_PREFERENCE>());

            // Remove visible frame border to avoid white outline
            int zero = 0;
            DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_VISIBLE_FRAME_BORDER_THICKNESS, ref zero, Marshal.SizeOf<int>());

            // Match dark mode to reduce contrast seams
            int dark = 1;
            DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, Marshal.SizeOf<int>());
        }
        catch
        {
        }
    }

    private static void CenterDialog(AppWindow dialog, AppWindow? parent)
    {
        try
        {
            var dlgSize = dialog.Size;
            if (parent is not null)
            {
                var pPos = parent.Position;
                var pSize = parent.Size;
                var x = pPos.X + (pSize.Width - dlgSize.Width) / 2;
                var y = pPos.Y + (pSize.Height - dlgSize.Height) / 2;
                dialog.Move(new PointInt32(x, y));
                return;
            }

            var displayArea = DisplayArea.GetFromWindowId(dialog.Id, DisplayAreaFallback.Primary);
            var work = displayArea.WorkArea;
            var fx = work.X + (work.Width - dlgSize.Width) / 2;
            var fy = work.Y + (work.Height - dlgSize.Height) / 2;
            dialog.Move(new PointInt32(fx, fy));
        }
        catch
        {
        }
    }

    private static AppWindow? TryGetActiveAppWindowOfCurrentProcess()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                return null;
            }
            if (!IsWindowFromCurrentProcess(hwnd))
            {
                return null;
            }
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }
        catch
        {
            return null;
        }
    }

    private static AppWindow? GetAppWindowFromCurrentWindow()
    {
        try
        {
            var hwnd = GetActiveWindow();
            if (hwnd == IntPtr.Zero)
            {
                return null;
            }
            if (!IsWindowFromCurrentProcess(hwnd))
            {
                return null;
            }
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }
        catch
        {
            return null;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    private static bool IsWindowFromCurrentProcess(IntPtr hwnd)
    {
        try
        {
            GetWindowThreadProcessId(hwnd, out var pid);
            return pid == (uint)Environment.ProcessId;
        }
        catch
        {
            return false;
        }
    }

    private void TrySetOwnedWindow(Microsoft.UI.Xaml.Window dialog, AppWindow? ownerAppWindow, AppWindow dialogAppWindow)
    {
        try
        {
            if (ownerAppWindow is null)
            {
                return;
            }

            // Get HWNDs
            var dlgHwnd = WindowNative.GetWindowHandle(dialog);
            var ownerHwnd = GetHwndFromAppWindow(ownerAppWindow);
            if (dlgHwnd == IntPtr.Zero || ownerHwnd == IntPtr.Zero)
            {
                return;
            }

            // Set owner and always-on-top over owner
            SetWindowLongPtr(dlgHwnd, GWL_HWNDPARENT, ownerHwnd);
            SetWindowPos(dlgHwnd, ownerHwnd, 0, 0, 0, 0, SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

            // Subclass owner to recenter dialog when owner moves (for reliability beyond AppWindow.Changed)
            _ownerHwnd = ownerHwnd;
            _ownerSubclassProc = OwnerWndProc;
            _ownerSubclassHandle = GCHandle.Alloc(this);
            _ownerSubclassHandleAllocated = true;
            SetWindowSubclass(_ownerHwnd, _ownerSubclassProc, _ownerSubclassId, IntPtr.Zero);
        }
        catch
        {
        }
    }

    private static IntPtr GetHwndFromAppWindow(AppWindow appWindow)
    {
        try
        {
            var id = appWindow.Id;
            return GetWindowFromWindowId(id);
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private static IntPtr GetWindowFromWindowId(Microsoft.UI.WindowId id)
    {
        try
        {
            return Microsoft.UI.Win32Interop.GetWindowFromWindowId(id);
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private const int GWL_HWNDPARENT = -8;
    const int GWL_STYLE = (-16);
    const int GWL_EXSTYLE = (-20);
    public const int WS_DLGFRAME = 0x00400000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    // Window subclassing to detect move/size of owner reliably and recenter dialog
    private delegate IntPtr SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, UIntPtr uIdSubclass, IntPtr dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool RemoveWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, UIntPtr uIdSubclass);

    private const int WM_MOVE = 0x0003;
    private const int WM_SIZE = 0x0005;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int VK_ESCAPE = 0x1B;

    private IntPtr OwnerWndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData)
    {
        try
        {
            if ((uMsg == WM_MOVE || uMsg == WM_SIZE) && _activeDialogWindow is not null)
            {
                try
                {
                    var dialogAppWindow = _activeDialogWindow.AppWindow;
                    CenterDialog(dialogAppWindow, _dialogOwner);
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private IntPtr DialogWndProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, UIntPtr uIdSubclass, IntPtr dwRefData)
    {
        try
        {
            if (_dismissWithEscapeForCurrentDialog && (uMsg == WM_KEYDOWN || uMsg == WM_SYSKEYDOWN))
            {
                if ((int)wParam == VK_ESCAPE)
                {
                    _ = CloseActiveDialog();
                    return IntPtr.Zero;
                }
            }
        }
        catch
        {
        }
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern IntPtr DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    // DWM corner and border styling
    private enum DWMWINDOWATTRIBUTE
    {
        DWMWA_WINDOW_CORNER_PREFERENCE = 33,
        DWMWA_BORDER_COLOR = 34,
        DWMWA_VISIBLE_FRAME_BORDER_THICKNESS = 37,
        DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
    }

    private enum DWM_WINDOW_CORNER_PREFERENCE
    {
        DWMWCP_DEFAULT = 0,
        DWMWCP_DONOTROUND = 1,
        DWMWCP_ROUND = 2,
        DWMWCP_ROUNDSMALL = 3
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref DWM_WINDOW_CORNER_PREFERENCE pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref uint pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref int pvAttribute, int cbAttribute);


    public static long GetWindowLong(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 4)
        {
            return GetWindowLong32(hWnd, nIndex);
        }
        return GetWindowLongPtr64(hWnd, nIndex);
    }

    [DllImport("User32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
    public static extern long GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("User32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Auto)]
    public static extern long GetWindowLongPtr64(IntPtr hWnd, int nIndex);

}


