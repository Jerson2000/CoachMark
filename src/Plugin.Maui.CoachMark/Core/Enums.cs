namespace Plugin.Maui.CoachMark;

/// <summary>
/// Specifies the preferred placement of the coach mark tooltip relative to its target element.
/// </summary>
public enum CoachMarkPlacement
{
    /// <summary>
    /// Automatically calculates the best placement based on available viewport space.
    /// </summary>
    Auto,

    /// <summary>
    /// Places the tooltip directly above the target spotlight.
    /// </summary>
    Top,

    /// <summary>
    /// Places the tooltip directly below the target spotlight.
    /// </summary>
    Bottom,

    /// <summary>
    /// Places the tooltip to the left of the target spotlight.
    /// </summary>
    Left,

    /// <summary>
    /// Places the tooltip to the right of the target spotlight.
    /// </summary>
    Right,

    /// <summary>
    /// Places the tooltip centered over the screen or target.
    /// </summary>
    Center
}

/// <summary>
/// Specifies the visual shape of the cutout spotlight.
/// </summary>
public enum SpotlightShape
{
    /// <summary>
    /// Rounded rectangle matching the element's bounding box plus padding and corner radius.
    /// </summary>
    RoundedRectangle,

    /// <summary>
    /// Sharp rectangle matching the element's bounding box plus padding.
    /// </summary>
    Rectangle,

    /// <summary>
    /// Perfect circle circumscribing or fitting the target element.
    /// </summary>
    Circle,

    /// <summary>
    /// Oval / Ellipse enclosing the target element.
    /// </summary>
    Oval
}

/// <summary>
/// Lifecycle states for the Coach Mark controller state machine.
/// </summary>
public enum CoachMarkState
{
    /// <summary>
    /// The coach mark tour is inactive.
    /// </summary>
    Idle,

    /// <summary>
    /// The coach mark is currently rendering and displaying a step.
    /// </summary>
    Showing,

    /// <summary>
    /// The coach mark is transitioning between steps.
    /// </summary>
    Transitioning,

    /// <summary>
    /// The coach mark is actively dismissing.
    /// </summary>
    Dismissing,

    /// <summary>
    /// The coach mark controller has been disposed.
    /// </summary>
    Disposed
}

/// <summary>
/// Indicates the reason a coach mark tour was dismissed.
/// </summary>
public enum CoachMarkDismissReason
{
    /// <summary>
    /// User completed all steps in the tour.
    /// </summary>
    Completed,

    /// <summary>
    /// User explicitly clicked the Skip button.
    /// </summary>
    Skipped,

    /// <summary>
    /// User tapped outside the spotlight with DismissOnOutsideTap enabled.
    /// </summary>
    OutsideTap,

    /// <summary>
    /// User tapped the spotlight itself.
    /// </summary>
    TargetTap,

    /// <summary>
    /// The tour was canceled via CancellationToken.
    /// </summary>
    Canceled,

    /// <summary>
    /// The host page or target element was unloaded/detached from the visual tree.
    /// </summary>
    TargetDetached,

    /// <summary>
    /// Programmatically dismissed via DismissAsync().
    /// </summary>
    Programmatic
}

/// <summary>
/// Specifies how a coach mark step locates its target region.
/// </summary>
public enum CoachMarkTargetKind
{
    /// <summary>
    /// Targets a MAUI VisualElement in the visual tree.
    /// </summary>
    VisualElement,

    /// <summary>
    /// Targets an absolute rectangle in the overlay coordinate space.
    /// </summary>
    AbsoluteRect,

    /// <summary>
    /// Targets a rectangle relative to a host VisualElement (e.g., a GraphicsView drawable region).
    /// </summary>
    RelativeRect,

    /// <summary>
    /// Targets native Shell chrome such as the flyout hamburger icon or a tab bar item.
    /// </summary>
    ShellElement
}

/// <summary>
/// Specifies the native Shell chrome element to highlight.
/// </summary>
public enum ShellTargetKind
{
    /// <summary>
    /// Targets the Shell flyout (hamburger) toggle button in the navigation bar.
    /// </summary>
    FlyoutIcon,

    /// <summary>
    /// Targets a Shell tab bar item by index, title, or route.
    /// </summary>
    TabBarItem,

    /// <summary>
    /// Targets a Shell flyout menu item inside the open sidebar drawer by index, title, or route.
    /// </summary>
    FlyoutItem
}

/// <summary>
/// Controls where the coach mark overlay is attached in the visual hierarchy.
/// </summary>
public enum CoachMarkOverlayHostMode
{
    /// <summary>
    /// Automatically uses Window hosting when any step targets Shell chrome, and Page hosting otherwise.
    /// </summary>
    Auto,

    /// <summary>
    /// Attaches the overlay inside the active ContentPage content grid.
    /// </summary>
    Page,

    /// <summary>
    /// Attaches the overlay at the platform Window root so it covers native Shell bars.
    /// </summary>
    Window
}
