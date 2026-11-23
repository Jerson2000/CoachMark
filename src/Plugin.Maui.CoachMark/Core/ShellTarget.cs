namespace Plugin.Maui.CoachMark;

/// <summary>
/// Describes a native Shell chrome target such as the flyout hamburger icon or a tab bar item.
/// </summary>
public sealed class ShellTarget
{
    /// <summary>
    /// Gets the kind of Shell chrome element to locate.
    /// </summary>
    public ShellTargetKind Kind { get; }

    /// <summary>
    /// Gets the zero-based tab index when targeting a tab bar item by index.
    /// </summary>
    public int? Index { get; }

    /// <summary>
    /// Gets the tab title or route when targeting a tab bar item by name.
    /// </summary>
    public string? TitleOrRoute { get; }

    private ShellTarget(ShellTargetKind kind, int? index, string? titleOrRoute)
    {
        Kind = kind;
        Index = index;
        TitleOrRoute = titleOrRoute;
    }

    /// <summary>
    /// Creates a target descriptor for the Shell flyout (hamburger) menu icon.
    /// </summary>
    public static ShellTarget FlyoutIcon() => new(ShellTargetKind.FlyoutIcon, null, null);

    /// <summary>
    /// Creates a target descriptor for a Shell tab bar item at the specified zero-based index.
    /// </summary>
    public static ShellTarget Tab(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        return new(ShellTargetKind.TabBarItem, index, null);
    }

    /// <summary>
    /// Creates a target descriptor for a Shell tab bar item matching the specified title or route.
    /// </summary>
    public static ShellTarget Tab(string titleOrRoute)
    {
        if (string.IsNullOrWhiteSpace(titleOrRoute))
        {
            throw new ArgumentException("Tab title or route cannot be null or whitespace.", nameof(titleOrRoute));
        }

        return new(ShellTargetKind.TabBarItem, null, titleOrRoute);
    }

    /// <summary>
    /// Creates a target descriptor for a Shell flyout sidebar item at the specified zero-based index.
    /// </summary>
    public static ShellTarget FlyoutItem(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        return new(ShellTargetKind.FlyoutItem, index, null);
    }

    /// <summary>
    /// Creates a target descriptor for a Shell flyout sidebar item matching the specified title or route.
    /// </summary>
    public static ShellTarget FlyoutItem(string titleOrRoute)
    {
        if (string.IsNullOrWhiteSpace(titleOrRoute))
        {
            throw new ArgumentException("Flyout item title or route cannot be null or whitespace.", nameof(titleOrRoute));
        }

        return new(ShellTargetKind.FlyoutItem, null, titleOrRoute);
    }
}
