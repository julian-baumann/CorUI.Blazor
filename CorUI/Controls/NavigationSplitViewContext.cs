using Microsoft.AspNetCore.Components;

namespace CorUI.Controls;

public record NavigationSplitViewContext(NavigationSplitView Owner)
{
    public EventCallback<bool> CollapsedChanged = Owner.CollapsedChanged;
    public RenderFragment? SidebarIcon => Owner.SidebarIcon;
    public bool IsSidebarCollapsed => Owner.IsSidebarCollapsed;
    public float CurrentSidebarWidth => Owner.CurrentSidebarWidth;
    public Task ExpandSidebarAsync() => Owner.SetSidebarCollapsedFromChildAsync(false);
    public Task CollapseSidebarAsync() => Owner.SetSidebarCollapsedFromChildAsync(true);
    public Task ToggleSidebarAsync() => Owner.ToggleSidebarFromChildAsync();
}
