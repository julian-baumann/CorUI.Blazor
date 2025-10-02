namespace CorUI.macOS;

internal sealed class DraggableVisualEffectView : NSVisualEffectView
{
    internal static readonly nfloat DefaultDragRegionHeight = 60f;

    public DraggableVisualEffectView(CGRect frame) : base(frame)
    {
        DragRegionHeight = DefaultDragRegionHeight;
    }

    public nfloat DragRegionHeight { get; set; }
}
