namespace Plugin.Maui.CoachMark;

/// <summary>
/// Monitors visual element layout, scrolling, and hierarchy changes in an event-driven, throttled manner.
/// </summary>
public interface IElementTracker : IDisposable
{
    /// <summary>
    /// Event raised when the tracked element's position, size, or visibility changes.
    /// </summary>
    event EventHandler? TargetLayoutChanged;

    /// <summary>
    /// Starts tracking the specified target element and its visual ancestors.
    /// </summary>
    void Track(VisualElement target);

    /// <summary>
    /// Stops tracking and unsubscribes from all event sources.
    /// </summary>
    void Stop();
}
