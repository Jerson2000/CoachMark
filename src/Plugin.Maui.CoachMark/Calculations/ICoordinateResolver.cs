namespace Plugin.Maui.CoachMark;

/// <summary>
/// Defines coordinate conversion from an arbitrary nested VisualElement to the root overlay coordinate space.
/// </summary>
public interface ICoordinateResolver
{
    /// <summary>
    /// Computes the bounding rectangle of the target element relative to the ancestor container or window root.
    /// </summary>
    Rect? GetBoundingBox(VisualElement target, VisualElement? relativeTo = null);
}
