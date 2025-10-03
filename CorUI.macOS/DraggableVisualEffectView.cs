using System.Runtime.InteropServices;

namespace CorUI.macOS;

internal sealed class DraggableVisualEffectView(CGRect frame, Window window) : NSVisualEffectView(frame)
{
    internal readonly NFloat DefaultDragRegionHeight = window.MacWindowOptions.MacTrafficLightStyle == MacTrafficLightStyle.Expanded ? 52 : 32;

    public NFloat DragRegionHeight => DefaultDragRegionHeight;
}
