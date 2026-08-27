namespace Plugin.Maui.CoachMark;

/// <summary>
/// Represents the result of a coach mark tour execution.
/// </summary>
public sealed class CoachMarkResult
{
    /// <summary>
    /// Gets the reason the tour was dismissed or completed.
    /// </summary>
    public CoachMarkDismissReason Reason { get; }

    /// <summary>
    /// Gets the 0-based index of the step that was active when dismissed.
    /// </summary>
    public int LastStepIndex { get; }

    /// <summary>
    /// Gets the total number of steps in the tour.
    /// </summary>
    public int TotalSteps { get; }

    /// <summary>
    /// Gets a value indicating whether all steps in the tour were completed normally.
    /// </summary>
    public bool IsCompleted => Reason == CoachMarkDismissReason.Completed;

    /// <summary>
    /// Gets a value indicating whether the tour was cancelled or interrupted.
    /// </summary>
    public bool IsCanceled => Reason == CoachMarkDismissReason.Canceled;

    public CoachMarkResult(CoachMarkDismissReason reason, int lastStepIndex, int totalSteps)
    {
        Reason = reason;
        LastStepIndex = lastStepIndex;
        TotalSteps = totalSteps;
    }
}
