namespace Plugin.Maui.CoachMark;

/// <summary>
/// Service interface for launching, controlling, and dismissing guided coach mark tours in .NET MAUI.
/// </summary>
public interface ICoachMarkService
{
    /// <summary>
    /// Gets a value indicating whether a coach mark tour is currently active on screen.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the current active step index (0-based) or -1 if inactive.
    /// </summary>
    int CurrentStepIndex { get; }

    /// <summary>
    /// Starts a multi-step coach mark guided tour.
    /// </summary>
    Task<CoachMarkResult> ShowAsync(
        IReadOnlyList<CoachMarkStep> steps,
        CoachMarkOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Displays a single standalone coach mark step.
    /// </summary>
    Task<CoachMarkResult> ShowAsync(
        CoachMarkStep step,
        CoachMarkOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually advances the tour to the next step.
    /// </summary>
    Task NextAsync();

    /// <summary>
    /// Manually returns the tour to the previous step.
    /// </summary>
    Task PreviousAsync();

    /// <summary>
    /// Skips and dismisses the remaining steps of the active tour.
    /// </summary>
    Task SkipAsync();

    /// <summary>
    /// Dismisses the active coach mark tour.
    /// </summary>
    Task DismissAsync(CoachMarkDismissReason reason = CoachMarkDismissReason.Programmatic);
}
