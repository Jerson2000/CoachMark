using Microsoft.Maui.Dispatching;
using Plugin.Maui.CoachMark;

namespace Plugin.Maui.CoachMark.Tests;

public class TestDispatcher : IDispatcher
{
    public bool IsDispatchRequired => false;

    public IDispatcherTimer CreateTimer() => throw new NotImplementedException();

    public bool Dispatch(Action action)
    {
        action();
        return true;
    }

    public bool DispatchDelayed(TimeSpan delay, Action action)
    {
        action();
        return true;
    }
}

public class CoachMarkStateAndResultTests
{
    [Fact]
    public void CoachMarkResult_Properties_ReflectState()
    {
        var completedResult = new CoachMarkResult(CoachMarkDismissReason.Completed, 2, 3);
        Assert.True(completedResult.IsCompleted);
        Assert.False(completedResult.IsCanceled);
        Assert.Equal(2, completedResult.LastStepIndex);
        Assert.Equal(3, completedResult.TotalSteps);

        var canceledResult = new CoachMarkResult(CoachMarkDismissReason.Canceled, 1, 3);
        Assert.False(canceledResult.IsCompleted);
        Assert.True(canceledResult.IsCanceled);

        var skippedResult = new CoachMarkResult(CoachMarkDismissReason.Skipped, 0, 3);
        Assert.False(skippedResult.IsCompleted);
        Assert.Equal(CoachMarkDismissReason.Skipped, skippedResult.Reason);
    }

    [Fact]
    public async Task StartTourAsync_EmptyStepsList_ReturnsCompletedWithoutThrowing()
    {
        var dispatcher = new TestDispatcher();
        using var controller = new CoachMarkController(dispatcher);

        var result = await controller.StartTourAsync(Array.Empty<CoachMarkStep>());

        Assert.NotNull(result);
        Assert.Equal(CoachMarkDismissReason.Completed, result.Reason);
        Assert.Equal(0, result.TotalSteps);
    }
}
