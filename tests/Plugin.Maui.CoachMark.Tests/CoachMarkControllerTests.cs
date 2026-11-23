using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Plugin.Maui.CoachMark;

namespace Plugin.Maui.CoachMark.Tests;

public class CoachMarkControllerTests
{
    [Fact]
    public void GetActiveContentPage_SingleContentPage_ReturnsPage()
    {
        var page = new ContentPage { Title = "Home" };
        var active = CoachMarkController.GetActiveContentPage(page);

        Assert.Same(page, active);
    }

    [Fact]
    public void GetActiveContentPage_NavigationPage_ReturnsCurrentPage()
    {
        var root = new ContentPage { Title = "Root" };
        var navPage = new NavigationPage(root);

        var active = CoachMarkController.GetActiveContentPage(navPage);

        Assert.Same(root, active);
    }

    [Fact]
    public void GetActiveContentPage_NestedNavigationPage_ReturnsDeepestCurrentPage()
    {
        var leafPage = new ContentPage { Title = "Leaf" };
        var innerNav = new NavigationPage(leafPage);
        var active = CoachMarkController.GetActiveContentPage(innerNav);

        Assert.Same(leafPage, active);
    }

    [Fact]
    public void GetActiveContentPage_FlyoutPage_ReturnsDetailPage()
    {
        var flyout = new ContentPage { Title = "Menu" };
        var detail = new ContentPage { Title = "Detail Content" };
        var flyoutPage = new FlyoutPage
        {
            Flyout = flyout,
            Detail = detail
        };

        var active = CoachMarkController.GetActiveContentPage(flyoutPage);

        Assert.Same(detail, active);
    }

    [Fact]
    public void GetActiveContentPage_NullRoot_ReturnsNull()
    {
        var active = CoachMarkController.GetActiveContentPage(null);
        Assert.Null(active);
    }

    [Fact]
    public void PlacementResult_RecordStructEquality_BehavesCorrectly()
    {
        var r1 = new PlacementResult(new Rect(10, 10, 100, 50), CoachMarkPlacement.Bottom, new Point(50, 60), 25.0);
        var r2 = new PlacementResult(new Rect(10, 10, 100, 50), CoachMarkPlacement.Bottom, new Point(50, 60), 25.0);
        var r3 = new PlacementResult(new Rect(0, 0, 100, 50), CoachMarkPlacement.Top, new Point(0, 0), 10.0);

        Assert.Equal(r1, r2);
        Assert.True(r1 == r2);
        Assert.False(r1 == r3);
        Assert.Equal(r1.GetHashCode(), r2.GetHashCode());
    }

    [Fact]
    public void ResolveEffectiveHostMode_AutoAndExplicit_BehavesCorrectly()
    {
        var button = new Button();
        var visualSteps = new[] { new CoachMarkStep(button, "A", "B") };
        var mixedSteps = new[]
        {
            new CoachMarkStep(button, "A", "B"),
            new CoachMarkStep(ShellTarget.FlyoutIcon(), "Menu", "Open menu")
        };

        var autoOptions = new CoachMarkOptions { OverlayHostMode = CoachMarkOverlayHostMode.Auto };
        Assert.Equal(CoachMarkOverlayHostMode.Page, CoachMarkController.ResolveEffectiveHostMode(visualSteps, autoOptions));
        Assert.Equal(CoachMarkOverlayHostMode.Window, CoachMarkController.ResolveEffectiveHostMode(mixedSteps, autoOptions));

        var forcedPage = new CoachMarkOptions { OverlayHostMode = CoachMarkOverlayHostMode.Page };
        Assert.Equal(CoachMarkOverlayHostMode.Page, CoachMarkController.ResolveEffectiveHostMode(mixedSteps, forcedPage));

        var forcedWindow = new CoachMarkOptions { OverlayHostMode = CoachMarkOverlayHostMode.Window };
        Assert.Equal(CoachMarkOverlayHostMode.Window, CoachMarkController.ResolveEffectiveHostMode(visualSteps, forcedWindow));
    }

    [Fact]
    public async Task ResolveStepBoundsAsync_RelativeRect_OffsetsByAnchorBoundsAndTracksAnchor()
    {
        var tracker = new StubElementTracker();
        var coordResolver = new StubCoordinateResolver(new Rect(50, 120, 300, 140));
        var shellResolver = new StubShellTargetResolver(null);
        var overlayHost = new StubOverlayHost();

        using var controller = new CoachMarkController(
            new StubDispatcher(),
            coordResolver,
            new TooltipPlacementEngine(),
            tracker,
            shellResolver,
            overlayHost);

        var canvas = new GraphicsView();
        var step = new CoachMarkStep(canvas, new Rect(15, 25, 80, 40), "Chart Bar", "Peak region");
        var options = new CoachMarkOptions { ScrollToTargetIfNeeded = false };

        var bounds = await controller.ResolveStepBoundsAsync(step, options, null, null, allowScroll: false);

        Assert.NotNull(bounds);
        Assert.Equal(new Rect(65, 145, 80, 40), bounds.Value);
        Assert.Same(canvas, tracker.LastTracked);
    }

    [Fact]
    public async Task ResolveStepBoundsAsync_AbsoluteRect_ResolvesWithoutElementAndStopsTracking()
    {
        var tracker = new StubElementTracker();
        using var controller = new CoachMarkController(
            new StubDispatcher(),
            new StubCoordinateResolver(null),
            new TooltipPlacementEngine(),
            tracker,
            new StubShellTargetResolver(null),
            new StubOverlayHost());

        var targetRect = new Rect(24, 48, 110, 44);
        var step = new CoachMarkStep(targetRect, "Abs", "Absolute coordinates");
        var options = new CoachMarkOptions { ScrollToTargetIfNeeded = false };

        var bounds = await controller.ResolveStepBoundsAsync(step, options, null, null, allowScroll: false);

        Assert.Equal(targetRect, bounds);
        Assert.True(tracker.StopCount > 0);
    }

    [Fact]
    public async Task ResolveStepBoundsAsync_ShellElement_UsesShellTargetResolver()
    {
        var tracker = new StubElementTracker();
        var expectedRect = new Rect(8, 12, 44, 44);
        var shellResolver = new StubShellTargetResolver(expectedRect);

        using var controller = new CoachMarkController(
            new StubDispatcher(),
            new StubCoordinateResolver(null),
            new TooltipPlacementEngine(),
            tracker,
            shellResolver,
            new StubOverlayHost());

        var shellTarget = ShellTarget.Tab("Details");
        var step = new CoachMarkStep(shellTarget, "Details Tab", "Switch to details");
        var options = new CoachMarkOptions { ScrollToTargetIfNeeded = false };

        var bounds = await controller.ResolveStepBoundsAsync(step, options, null, null, allowScroll: false);

        Assert.Equal(expectedRect, bounds);
        Assert.Same(shellTarget, shellResolver.LastTarget);
        Assert.True(tracker.StopCount > 0);
    }

    [Fact]
    public async Task ResolveStepBoundsAsync_RelativeRectWithMissingAnchor_ReturnsNull()
    {
        using var controller = new CoachMarkController(
            new StubDispatcher(),
            new StubCoordinateResolver(null),
            new TooltipPlacementEngine(),
            new StubElementTracker(),
            new StubShellTargetResolver(null),
            new StubOverlayHost());

        var canvas = new GraphicsView();
        var step = new CoachMarkStep(canvas, new Rect(10, 10, 50, 50), "Missing", "Anchor invisible");

        var bounds = await controller.ResolveStepBoundsAsync(step, new CoachMarkOptions(), null, null, allowScroll: false);

        Assert.Null(bounds);
    }

    [Fact]
    public async Task NextAndPrevious_SkipUnresolvableStepsAndNeverStall()
    {
        var page = new ContentPage();
        var shellResolver = new DynamicShellTargetResolver(target =>
            target.Kind == ShellTargetKind.FlyoutIcon ? new Rect(10, 10, 40, 40) : null);

        using var controller = new CoachMarkController(
            new StubDispatcher(),
            new StubCoordinateResolver(new Rect(20, 20, 100, 40)),
            new TooltipPlacementEngine(),
            new StubElementTracker(),
            shellResolver,
            new StubOverlayHost())
        {
            ActivePageResolver = () => page
        };

        var steps = new[]
        {
            new CoachMarkStep(ShellTarget.FlyoutIcon(), "Step 0", "Flyout"),
            new CoachMarkStep(ShellTarget.Tab(0), "Step 1 (Unresolvable)", "Skipped"),
            new CoachMarkStep(new Rect(50, 80, 120, 50), "Step 2", "Absolute Rect"),
            new CoachMarkStep(ShellTarget.Tab(1), "Step 3 (Unresolvable)", "Also skipped")
        };

        var options = new CoachMarkOptions
        {
            EnableAnimations = false,
            ScrollToTargetIfNeeded = false,
            TooltipTemplate = new DataTemplate(() => new BoxView { WidthRequest = 200, HeightRequest = 100 })
        };

        var tourTask = controller.StartTourAsync(steps, options);

        Assert.True(controller.IsActive);
        Assert.Equal(0, controller.CurrentStepIndex);

        // Step 1 is unresolvable -> NextAsync should skip to Step 2
        await controller.NextAsync();
        Assert.True(controller.IsActive);
        Assert.Equal(2, controller.CurrentStepIndex);

        // Step 1 is unresolvable -> PreviousAsync should skip back to Step 0
        await controller.PreviousAsync();
        Assert.True(controller.IsActive);
        Assert.Equal(0, controller.CurrentStepIndex);

        // Advance back to Step 2, then NextAsync should skip Step 3 and complete
        await controller.NextAsync();
        Assert.Equal(2, controller.CurrentStepIndex);

        await controller.NextAsync();
        var result = await tourTask;
        Assert.False(controller.IsActive);
        Assert.Equal(CoachMarkDismissReason.Completed, result.Reason);
        Assert.Equal(2, result.LastStepIndex);
    }

    [Fact]
    public async Task OverlayView_WithHostViewportSizeFallback_PlacesTooltipWithinViewport()
    {
        var overlay = new CoachMarkOverlayView
        {
            HostViewportSize = new Size(800, 600)
        };

        var step = new CoachMarkStep(ShellTarget.FlyoutIcon(), "Menu", "Open menu")
        {
            Placement = CoachMarkPlacement.Bottom
        };

        var options = new CoachMarkOptions
        {
            EnableAnimations = false,
            TooltipTemplate = new DataTemplate(() => new BoxView { WidthRequest = 240, HeightRequest = 120 })
        };

        Assert.True(overlay.Width <= 0);
        Assert.True(overlay.Height <= 0);

        await overlay.TransitionToStepAsync(
            step,
            0,
            1,
            new Rect(10, 10, 44, 44),
            options,
            new TooltipPlacementEngine(),
            CancellationToken.None);

        var tooltipBounds = overlay.CurrentTooltipBounds;
        Assert.NotEqual(Rect.Zero, tooltipBounds);
        Assert.True(tooltipBounds.Width > 0);
        Assert.True(tooltipBounds.Height > 0);
        Assert.True(tooltipBounds.X >= 0 && tooltipBounds.Right <= 800);
        Assert.True(tooltipBounds.Y >= 0 && tooltipBounds.Bottom <= 600);
    }

    [Fact]
    public async Task NextAsync_InvokesAfterDismissAsyncOncePerOutgoingStepAndResolvesFlyoutItem()
    {
        var page = new ContentPage();
        var flyoutItemRect = new Rect(12, 120, 220, 44);
        var shellResolver = new DynamicShellTargetResolver(target =>
            target.Kind == ShellTargetKind.FlyoutItem ? flyoutItemRect : new Rect(10, 10, 40, 40));

        using var controller = new CoachMarkController(
            new StubDispatcher(),
            new StubCoordinateResolver(new Rect(20, 20, 100, 40)),
            new TooltipPlacementEngine(),
            new StubElementTracker(),
            shellResolver,
            new StubOverlayHost())
        {
            ActivePageResolver = () => page
        };

        int step0DismissCount = 0;
        int step1DismissCount = 0;

        var flyoutItemTarget = ShellTarget.FlyoutItem("About");
        var steps = new[]
        {
            new CoachMarkStep(ShellTarget.FlyoutIcon(), "Menu", "Open menu")
            {
                AfterDismissAsync = () =>
                {
                    step0DismissCount++;
                    return Task.CompletedTask;
                }
            },
            new CoachMarkStep(flyoutItemTarget, "About", "Sidebar item")
            {
                AfterDismissAsync = () =>
                {
                    step1DismissCount++;
                    return Task.CompletedTask;
                }
            }
        };

        var options = new CoachMarkOptions
        {
            EnableAnimations = false,
            ScrollToTargetIfNeeded = false,
            TooltipTemplate = new DataTemplate(() => new BoxView { WidthRequest = 200, HeightRequest = 100 })
        };

        var tourTask = controller.StartTourAsync(steps, options);
        Assert.Equal(0, controller.CurrentStepIndex);
        Assert.Equal(0, step0DismissCount);

        await controller.NextAsync();
        Assert.Equal(1, controller.CurrentStepIndex);
        Assert.Equal(1, step0DismissCount);
        Assert.Equal(0, step1DismissCount);

        var resolvedBounds = await controller.ResolveStepBoundsAsync(steps[1], options, null, null, allowScroll: false);
        Assert.Equal(flyoutItemRect, resolvedBounds);

        await controller.NextAsync();
        var result = await tourTask;
        Assert.Equal(CoachMarkDismissReason.Completed, result.Reason);
        Assert.Equal(1, step0DismissCount);
        Assert.Equal(1, step1DismissCount);
    }

    private sealed class DynamicShellTargetResolver(Func<ShellTarget, Rect?> resolver) : IShellTargetResolver
    {
        public Rect? GetBoundingBox(ShellTarget target, Window? window, VisualElement? relativeTo = null) => resolver(target);
    }

    private sealed class StubDispatcher : IDispatcher
    {
        public bool IsDispatchRequired => false;

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

        public IDispatcherTimer CreateTimer() => throw new NotSupportedException();
    }

    private sealed class StubCoordinateResolver(Rect? result) : ICoordinateResolver
    {
        public Rect? GetBoundingBox(VisualElement target, VisualElement? relativeTo = null) => result;
    }

    private sealed class StubShellTargetResolver(Rect? result) : IShellTargetResolver
    {
        public ShellTarget? LastTarget { get; private set; }

        public Rect? GetBoundingBox(ShellTarget target, Window? window, VisualElement? relativeTo = null)
        {
            LastTarget = target;
            return result;
        }
    }

    private sealed class StubElementTracker : IElementTracker
    {
        public event EventHandler? TargetLayoutChanged
        {
            add { }
            remove { }
        }

        public VisualElement? LastTracked { get; private set; }
        public int StopCount { get; private set; }

        public void Track(VisualElement target) => LastTracked = target;
        public void Stop() => StopCount++;
        public void Dispose() { }
    }

    private sealed class StubOverlayHost : IOverlayHost
    {
        public bool IsWindowHosted { get; private set; }

        public Task<bool> AttachAsync(CoachMarkOverlayView overlayView, ContentPage page, Window? window, CoachMarkOverlayHostMode mode)
        {
            IsWindowHosted = mode == CoachMarkOverlayHostMode.Window;
            ((IView)overlayView).Arrange(new Rect(0, 0, 800, 600));
            return Task.FromResult(true);
        }

        public Task DetachAsync()
        {
            IsWindowHosted = false;
            return Task.CompletedTask;
        }
    }
}
