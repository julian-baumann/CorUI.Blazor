namespace CorUI;

public record Window
{
    public required string ContentPath { get; init; }

    public string Title { get; init; } = "";

    public int Width { get; init; } = 800;
    public int Height { get; init; } = 600;
    
    public bool CanClose { get; init; } = true;
    public bool CanMinimize { get; init; } = true;
    public bool CanMaximize { get; init; } = true;
    public bool CanResize { get; init; } = true;
    
    public bool ShowCloseButton { get; init; } = true;
    public bool ShowMinimizeButton { get; init; } = true;
    public bool ShowMaximizeButton { get; init; } = true;

    public bool IsFullScreen { get; init; } = false;

    public MacWindowOptions MacWindowOptions { get; init; } = new();

    // Platform hints
    public bool EnableDrag { get; init; } = true;
    public bool DismissWithEscape { get; init; } = true;
}

public enum MacTrafficLightStyle
{
    Compact,
    Expanded
}

public record MacWindowOptions
{
    public MacTrafficLightStyle MacTrafficLightStyle { get; set; } = MacTrafficLightStyle.Expanded;
}