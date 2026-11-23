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

    [Fact]
    public void ShellTarget_Factories_ConstructAndValidateCorrectly()
    {
        var flyout = ShellTarget.FlyoutIcon();
        Assert.Equal(ShellTargetKind.FlyoutIcon, flyout.Kind);
        Assert.Null(flyout.Index);
        Assert.Null(flyout.TitleOrRoute);

        var tabByIndex = ShellTarget.Tab(2);
        Assert.Equal(ShellTargetKind.TabBarItem, tabByIndex.Kind);
        Assert.Equal(2, tabByIndex.Index);
        Assert.Null(tabByIndex.TitleOrRoute);

        var tabByName = ShellTarget.Tab("Details");
        Assert.Equal(ShellTargetKind.TabBarItem, tabByName.Kind);
        Assert.Null(tabByName.Index);
        Assert.Equal("Details", tabByName.TitleOrRoute);

        Assert.Throws<ArgumentOutOfRangeException>(() => ShellTarget.Tab(-1));
        Assert.Throws<ArgumentException>(() => ShellTarget.Tab(""));
        Assert.Throws<ArgumentException>(() => ShellTarget.Tab("   "));

        var flyoutByIndex = ShellTarget.FlyoutItem(1);
        Assert.Equal(ShellTargetKind.FlyoutItem, flyoutByIndex.Kind);
        Assert.Equal(1, flyoutByIndex.Index);
        Assert.Null(flyoutByIndex.TitleOrRoute);

        var flyoutByName = ShellTarget.FlyoutItem("About");
        Assert.Equal(ShellTargetKind.FlyoutItem, flyoutByName.Kind);
        Assert.Null(flyoutByName.Index);
        Assert.Equal("About", flyoutByName.TitleOrRoute);

        Assert.Throws<ArgumentOutOfRangeException>(() => ShellTarget.FlyoutItem(-1));
        Assert.Throws<ArgumentException>(() => ShellTarget.FlyoutItem(""));
        Assert.Throws<ArgumentException>(() => ShellTarget.FlyoutItem("   "));
    }

    [Fact]
    public async Task AbsoluteRect_StaticSyncAsync_ResolvesRect()
    {
        var staticRect = new Rect(10, 20, 100, 50);
        var staticStep = new CoachMarkStep(staticRect, "Static", "Desc");
        Assert.Equal(CoachMarkTargetKind.AbsoluteRect, staticStep.TargetKind);
        Assert.Equal(staticRect, await staticStep.ResolveTargetRectAsync());

        var syncRect = new Rect(30, 40, 80, 40);
        var syncStep = new CoachMarkStep(() => syncRect, "Sync", "Desc");
        Assert.Equal(CoachMarkTargetKind.AbsoluteRect, syncStep.TargetKind);
        Assert.Equal(syncRect, await syncStep.ResolveTargetRectAsync());

        var asyncRect = new Rect(50, 60, 120, 30);
        var asyncStep = new CoachMarkStep(async () =>
        {
            await Task.Yield();
            return (Rect?)asyncRect;
        }, "Async", "Desc");
        Assert.Equal(CoachMarkTargetKind.AbsoluteRect, asyncStep.TargetKind);
        Assert.Equal(asyncRect, await asyncStep.ResolveTargetRectAsync());
    }

    [Fact]
    public async Task RelativeRect_StaticSyncAsync_ResolvesHostAndLocalRect()
    {
        var canvas = new GraphicsView();
        var staticRect = new Rect(12, 16, 64, 32);
        var staticStep = new CoachMarkStep(canvas, staticRect, "Static Rel", "Desc");
        Assert.Equal(CoachMarkTargetKind.RelativeRect, staticStep.TargetKind);
        Assert.Same(canvas, await staticStep.ResolveTargetAsync());
        Assert.Equal(staticRect, await staticStep.ResolveTargetRectAsync());

        var syncStep = new CoachMarkStep(canvas, () => new Rect(5, 8, 40, 20), "Sync Rel", "Desc");
        Assert.Equal(CoachMarkTargetKind.RelativeRect, syncStep.TargetKind);
        Assert.Same(canvas, await syncStep.ResolveTargetAsync());
        Assert.Equal(new Rect(5, 8, 40, 20), await syncStep.ResolveTargetRectAsync());

        var asyncStep = new CoachMarkStep(canvas, async () =>
        {
            await Task.Yield();
            return (Rect?)new Rect(1, 2, 30, 15);
        }, "Async Rel", "Desc");
        Assert.Equal(CoachMarkTargetKind.RelativeRect, asyncStep.TargetKind);
        Assert.Same(canvas, await asyncStep.ResolveTargetAsync());
        Assert.Equal(new Rect(1, 2, 30, 15), await asyncStep.ResolveTargetRectAsync());
    }

    [Theory]
    [InlineData(0, 0, 0, 10)]
    [InlineData(0, 0, 10, 0)]
    [InlineData(0, 0, -5, 10)]
    [InlineData(0, 0, 10, -5)]
    [InlineData(double.NaN, 0, 10, 10)]
    [InlineData(0, 0, double.PositiveInfinity, 10)]
    public async Task ResolveTargetRectAsync_InvalidRect_ReturnsNull(double x, double y, double w, double h)
    {
        var step = new CoachMarkStep(new Rect(x, y, w, h), "Invalid", "Desc");
        var resolved = await step.ResolveTargetRectAsync();
        Assert.Null(resolved);
    }

    [Fact]
    public void ShellTargetStep_RetainsDescriptorAndTargetKind()
    {
        var shellTarget = ShellTarget.FlyoutIcon();
        var step = new CoachMarkStep(shellTarget, "Menu", "Open navigation menu");

        Assert.Equal(CoachMarkTargetKind.ShellElement, step.TargetKind);
        Assert.Same(shellTarget, step.ShellTarget);
        Assert.Throws<ArgumentNullException>(() => new CoachMarkStep((ShellTarget)null!, "T", "D"));
    }
}
