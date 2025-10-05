namespace CorUI;

public class Dialog
{
    public required string ContentPath { get; init; }

    public string Title { get; init; } = "";

    public int Width { get; init; } = 500;
    public int Height { get; init; } = 600;

    public int MinWidth { get; init; } = 0;
    public int MinHeight { get; init; } = 0;
    
    public int? MaxWidth { get; init; }
    public int? MaxHeight { get; init; }

    public bool BackdropDismissable { get; init; } = true;
    public bool DismissWithEscape { get; init; } = true;
}