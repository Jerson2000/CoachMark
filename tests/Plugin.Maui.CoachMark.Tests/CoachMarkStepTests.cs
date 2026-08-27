using Microsoft.Maui.Controls;
using Plugin.Maui.CoachMark;

namespace Plugin.Maui.CoachMark.Tests;

public class CoachMarkStepTests
{
    [Fact]
    public async Task ResolveTargetAsync_StaticTarget_ReturnsElement()
    {
        var button = new Button { Text = "Test" };
        var step = new CoachMarkStep(button, "Title", "Desc");

        var target = await step.ResolveTargetAsync();

        Assert.Same(button, target);
        Assert.Equal("Title", step.Title);
        Assert.Equal("Desc", step.Description);
    }

    [Fact]
    public async Task ResolveTargetAsync_DynamicAccessor_CallsAccessor()
    {
        var label = new Label { Text = "Dynamic" };
        var step = new CoachMarkStep(() => label, "Lazy Title", "Lazy Desc");

        var target = await step.ResolveTargetAsync();

        Assert.Same(label, target);
    }

    [Fact]
    public async Task ResolveTargetAsync_AsyncAccessor_AwaitsAndReturnsTarget()
    {
        var entry = new Entry();
        var step = new CoachMarkStep(async () =>
        {
            await Task.Yield();
            return (VisualElement)entry;
        }, "Async Title", "Async Desc");

        var target = await step.ResolveTargetAsync();

        Assert.Same(entry, target);
    }

    [Fact]
    public void Constructor_NullStaticTarget_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CoachMarkStep((VisualElement)null!, "T", "D"));
    }
}
