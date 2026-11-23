namespace Plugin.Maui.CoachMark;

/// <summary>
/// Resolves bounding rectangles for native Shell chrome elements such as the flyout icon and tab bar items.
/// </summary>
public interface IShellTargetResolver
{
    /// <summary>
    /// Computes the bounding rectangle of the specified Shell chrome target relative to an optional overlay element.
    /// </summary>
    Rect? GetBoundingBox(ShellTarget target, Window? window, VisualElement? relativeTo = null);
}
