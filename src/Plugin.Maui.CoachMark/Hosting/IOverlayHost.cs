namespace Plugin.Maui.CoachMark;

/// <summary>
/// Abstraction for attaching and detaching <see cref="CoachMarkOverlayView"/> to either a ContentPage or a platform Window.
/// </summary>
public interface IOverlayHost
{
    /// <summary>
    /// Gets a value indicating whether the currently attached overlay is hosted at the platform Window root.
    /// </summary>
    bool IsWindowHosted { get; }

    /// <summary>
    /// Attaches the overlay view according to the requested hosting mode.
    /// </summary>
    Task<bool> AttachAsync(
        CoachMarkOverlayView overlayView,
        ContentPage page,
        Window? window,
        CoachMarkOverlayHostMode mode);

    /// <summary>
    /// Detaches any currently attached overlay view and restores the host visual hierarchy.
    /// </summary>
    Task DetachAsync();
}
