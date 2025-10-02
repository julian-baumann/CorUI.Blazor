using WebKit;

namespace CorUI.macOS;

internal sealed class DraggableWKWebView : WKWebView
{
    private bool _trackingDragRegion;

    public DraggableWKWebView(CGRect frame, WKWebViewConfiguration configuration)
        : base(frame, configuration)
    {
    }

    public nfloat DragRegionHeight { get; set; } = DraggableVisualEffectView.DefaultDragRegionHeight;

    public override void MouseDown(NSEvent theEvent)
    {
        var location = ConvertPointFromView(theEvent.LocationInWindow, null);
        if (!Bounds.Contains(location))
        {
            _trackingDragRegion = false;
            base.MouseDown(theEvent);
            return;
        }

        var distanceFromTop = IsFlipped ? location.Y : Bounds.Height - location.Y;
        _trackingDragRegion = distanceFromTop <= DragRegionHeight;
        base.MouseDown(theEvent);
    }

    public override void MouseDragged(NSEvent theEvent)
    {
        if (_trackingDragRegion && Window is { } window)
        {
            window.PerformWindowDrag(theEvent);
            return;
        }

        base.MouseDragged(theEvent);
    }

    public override void MouseUp(NSEvent theEvent)
    {
        _trackingDragRegion = false;
        base.MouseUp(theEvent);
    }
}
