using Microsoft.Extensions.DependencyInjection;
using ObjCRuntime;

namespace CorUI.macOS;

[Register("AppDelegate")]
public sealed class AppDelegate(IServiceProvider serviceProvider) : NSApplicationDelegate
{
    private readonly HashSet<BlazorWindowController> _openWindows = new();

    public override void DidFinishLaunching(NSNotification notification)
    {
        ConfigureMainMenu();
        CreateAndShowMainWindow();
    }

    private void ConfigureMainMenu()
    {
        var app = NSApplication.SharedApplication;
        var mainMenu = new NSMenu();

        mainMenu.AddItem(CreateApplicationMenu());
        mainMenu.AddItem(CreateEditMenu());
        mainMenu.AddItem(CreateWindowMenu());

        app.MainMenu = mainMenu;
    }

    private void CreateAndShowMainWindow()
    {
        var corApp = serviceProvider.GetRequiredService<ICorUIApplication>();

        var appStartWindow = corApp.StartWindow;
        var controller = new BlazorWindowController(OnWindowClosed, appStartWindow, serviceProvider);
        _openWindows.Add(controller);
        controller.ShowWindow(this);
    }

    private void OnWindowClosed(BlazorWindowController controller)
    {
        if (_openWindows.Remove(controller) && _openWindows.Count == 0)
        {
            NSApplication.SharedApplication.UpdateWindows();
        }

        controller.Dispose();
    }

    public override bool ApplicationShouldHandleReopen(NSApplication sender, bool hasVisibleWindows)
    {
        if (!hasVisibleWindows)
        {
            CreateAndShowMainWindow();
        }

        return true;
    }

    public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender) => false;

    private NSMenuItem CreateApplicationMenu()
    {
        var appName = NSProcessInfo.ProcessInfo.ProcessName;
        var appMenu = new NSMenu(appName);

        appMenu.AddItem(CreateMenuItem($"About {appName}", string.Empty, new Selector("orderFrontStandardAboutPanel:"), NSApplication.SharedApplication));
        appMenu.AddItem(NSMenuItem.SeparatorItem);

        var servicesMenu = new NSMenu("Services");
        var servicesItem = new NSMenuItem("Services")
        {
            Submenu = servicesMenu
        };
        appMenu.AddItem(servicesItem);
        NSApplication.SharedApplication.ServicesMenu = servicesMenu;

        appMenu.AddItem(NSMenuItem.SeparatorItem);
        appMenu.AddItem(CreateMenuItem($"Hide {appName}", "h", new Selector("hide:"), NSApplication.SharedApplication));
        appMenu.AddItem(CreateMenuItem("Hide Others", "h", new Selector("hideOtherApplications:"), NSApplication.SharedApplication, NSEventModifierMask.CommandKeyMask | NSEventModifierMask.AlternateKeyMask));
        appMenu.AddItem(CreateMenuItem("Show All", string.Empty, new Selector("unhideAllApplications:"), NSApplication.SharedApplication));
        appMenu.AddItem(NSMenuItem.SeparatorItem);
        appMenu.AddItem(CreateMenuItem($"Quit {appName}", "q", new Selector("terminate:"), NSApplication.SharedApplication));

        var container = new NSMenuItem(string.Empty)
        {
            Submenu = appMenu
        };

        return container;
    }

    private NSMenuItem CreateEditMenu()
    {
        var editMenu = new NSMenu("Edit");

        editMenu.AddItem(CreateMenuItem("Undo", "z", new Selector("undo:"), null));
        editMenu.AddItem(CreateMenuItem("Redo", "Z", new Selector("redo:"), null, NSEventModifierMask.CommandKeyMask | NSEventModifierMask.ShiftKeyMask));
        editMenu.AddItem(NSMenuItem.SeparatorItem);
        editMenu.AddItem(CreateMenuItem("Cut", "x", new Selector("cut:"), null));
        editMenu.AddItem(CreateMenuItem("Copy", "c", new Selector("copy:"), null));
        editMenu.AddItem(CreateMenuItem("Paste", "v", new Selector("paste:"), null));
        editMenu.AddItem(CreateMenuItem("Delete", string.Empty, new Selector("delete:"), null));
        editMenu.AddItem(CreateMenuItem("Select All", "a", new Selector("selectAll:"), null));

        var container = new NSMenuItem("Edit")
        {
            Submenu = editMenu
        };

        return container;
    }

    private NSMenuItem CreateWindowMenu()
    {
        var windowMenu = new NSMenu("Window");

        windowMenu.AddItem(CreateMenuItem("Close Window", "w", new Selector("performClose:"), null));
        windowMenu.AddItem(CreateMenuItem("Minimize", "m", new Selector("performMiniaturize:"), null));
        windowMenu.AddItem(CreateMenuItem("Zoom", string.Empty, new Selector("performZoom:"), null));
        windowMenu.AddItem(NSMenuItem.SeparatorItem);
        windowMenu.AddItem(CreateMenuItem("Bring All to Front", string.Empty, new Selector("arrangeInFront:"), null));

        NSApplication.SharedApplication.WindowsMenu = windowMenu;

        var container = new NSMenuItem("Window")
        {
            Submenu = windowMenu
        };

        return container;
    }

    private static NSMenuItem CreateMenuItem(string title, string keyEquivalent, Selector action, NSObject? target, NSEventModifierMask modifiers = NSEventModifierMask.CommandKeyMask)
    {
        var item = new NSMenuItem(title, action, keyEquivalent);

        if (target is not null)
        {
            item.Target = target;
        }

        if (!string.IsNullOrEmpty(keyEquivalent))
        {
            item.KeyEquivalentModifierMask = modifiers;
        }

        return item;
    }
}
