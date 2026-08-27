namespace Plugin.Maui.CoachMark;

/// <summary>
/// Production implementation of ICoachMarkService.
/// </summary>
public sealed class CoachMarkService : ICoachMarkService, IDisposable
{
    private readonly CoachMarkController _controller;

    public bool IsActive => _controller.IsActive;
    public int CurrentStepIndex => _controller.CurrentStepIndex;

    public CoachMarkService(IDispatcher dispatcher)
    {
        _controller = new CoachMarkController(dispatcher);
    }

    public CoachMarkService(CoachMarkController controller)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public Task<CoachMarkResult> ShowAsync(
        IReadOnlyList<CoachMarkStep> steps,
        CoachMarkOptions? options = null,
        CancellationToken cancellationToken = default) =>
        _controller.StartTourAsync(steps, options, cancellationToken);

    public Task<CoachMarkResult> ShowAsync(
        CoachMarkStep step,
        CoachMarkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(step);
        return _controller.StartTourAsync([step], options, cancellationToken);
    }

    public Task NextAsync() => _controller.NextAsync();

    public Task PreviousAsync() => _controller.PreviousAsync();

    public Task SkipAsync() => _controller.SkipAsync();

    public Task DismissAsync(CoachMarkDismissReason reason = CoachMarkDismissReason.Programmatic) =>
        _controller.DismissAsync(reason);

    public void Dispose() => _controller.Dispose();
}
