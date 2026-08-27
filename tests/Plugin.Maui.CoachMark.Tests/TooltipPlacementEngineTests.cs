using Microsoft.Maui.Graphics;
using Plugin.Maui.CoachMark;

namespace Plugin.Maui.CoachMark.Tests;

public class TooltipPlacementEngineTests
{
    private readonly TooltipPlacementEngine _engine = new();

    [Fact]
    public void CalculatePlacement_PreferredBottom_WithSufficientSpace_PlacesBelowTarget()
    {
        // Viewport 400x800, target in upper middle: (150, 100, 100, 50)
        var targetBounds = new Rect(150, 100, 100, 50);
        var tooltipSize = new Size(200, 100);
        var containerBounds = new Rect(0, 0, 400, 800);
        var safeMargin = new Thickness(16);
        double spacing = 12.0;

        var result = _engine.CalculatePlacement(
            targetBounds,
            tooltipSize,
            containerBounds,
            safeMargin,
            spacing,
            CoachMarkPlacement.Bottom);

        Assert.Equal(CoachMarkPlacement.Bottom, result.EffectivePlacement);
        Assert.Equal(targetBounds.Bottom + spacing, result.TooltipBounds.Y);
        // Tooltip should be horizontally centered on target (TargetCenterX = 200, TooltipWidth = 200 => X = 100)
        Assert.Equal(100, result.TooltipBounds.X);
    }

    [Fact]
    public void CalculatePlacement_PreferredBottom_WhenInsufficientSpaceBelow_FallsBackToTop()
    {
        // Viewport 400x800, target near bottom: (150, 720, 100, 50)
        // Space below = 800 - 16 - 770 = 14px (not enough for 100px height)
        var targetBounds = new Rect(150, 720, 100, 50);
        var tooltipSize = new Size(200, 100);
        var containerBounds = new Rect(0, 0, 400, 800);
        var safeMargin = new Thickness(16);
        double spacing = 12.0;

        var result = _engine.CalculatePlacement(
            targetBounds,
            tooltipSize,
            containerBounds,
            safeMargin,
            spacing,
            CoachMarkPlacement.Bottom);

        Assert.Equal(CoachMarkPlacement.Top, result.EffectivePlacement);
        Assert.Equal(targetBounds.Top - spacing - tooltipSize.Height, result.TooltipBounds.Y);
    }

    [Fact]
    public void CalculatePlacement_PreferredTop_WithSufficientSpace_PlacesAboveTarget()
    {
        var targetBounds = new Rect(150, 400, 100, 50);
        var tooltipSize = new Size(200, 100);
        var containerBounds = new Rect(0, 0, 400, 800);
        var safeMargin = new Thickness(16);
        double spacing = 12.0;

        var result = _engine.CalculatePlacement(
            targetBounds,
            tooltipSize,
            containerBounds,
            safeMargin,
            spacing,
            CoachMarkPlacement.Top);

        Assert.Equal(CoachMarkPlacement.Top, result.EffectivePlacement);
        Assert.Equal(targetBounds.Top - spacing - tooltipSize.Height, result.TooltipBounds.Y);
    }

    [Fact]
    public void CalculatePlacement_PreferredRight_PlacesRightOfTarget()
    {
        var targetBounds = new Rect(50, 200, 50, 50);
        var tooltipSize = new Size(150, 80);
        var containerBounds = new Rect(0, 0, 400, 800);
        var safeMargin = new Thickness(16);
        double spacing = 12.0;

        var result = _engine.CalculatePlacement(
            targetBounds,
            tooltipSize,
            containerBounds,
            safeMargin,
            spacing,
            CoachMarkPlacement.Right);

        Assert.Equal(CoachMarkPlacement.Right, result.EffectivePlacement);
        Assert.Equal(targetBounds.Right + spacing, result.TooltipBounds.X);
    }

    [Fact]
    public void CalculatePlacement_PreferredLeft_PlacesLeftOfTarget()
    {
        var targetBounds = new Rect(300, 200, 50, 50);
        var tooltipSize = new Size(150, 80);
        var containerBounds = new Rect(0, 0, 400, 800);
        var safeMargin = new Thickness(16);
        double spacing = 12.0;

        var result = _engine.CalculatePlacement(
            targetBounds,
            tooltipSize,
            containerBounds,
            safeMargin,
            spacing,
            CoachMarkPlacement.Left);

        Assert.Equal(CoachMarkPlacement.Left, result.EffectivePlacement);
        Assert.Equal(targetBounds.Left - spacing - tooltipSize.Width, result.TooltipBounds.X);
    }

    [Fact]
    public void CalculatePlacement_ClampsTooltip_WithinSafeMargin()
    {
        // Target is at the very right edge (380, 100, 20, 20)
        var targetBounds = new Rect(380, 100, 20, 20);
        var tooltipSize = new Size(200, 100);
        var containerBounds = new Rect(0, 0, 400, 800);
        var safeMargin = new Thickness(16);
        double spacing = 12.0;

        var result = _engine.CalculatePlacement(
            targetBounds,
            tooltipSize,
            containerBounds,
            safeMargin,
            spacing,
            CoachMarkPlacement.Bottom);

        // Tooltip right edge must not exceed 400 - 16 = 384
        Assert.True(result.TooltipBounds.Right <= containerBounds.Right - safeMargin.Right + 0.01);
        Assert.True(result.TooltipBounds.Left >= safeMargin.Left);
    }

    [Fact]
    public void CalculatePlacement_ArrowOffset_ClampedWithinTooltipCardEdges()
    {
        var targetBounds = new Rect(10, 100, 20, 20);
        var tooltipSize = new Size(200, 100);
        var containerBounds = new Rect(0, 0, 400, 800);
        var safeMargin = new Thickness(16);
        double spacing = 12.0;

        var result = _engine.CalculatePlacement(
            targetBounds,
            tooltipSize,
            containerBounds,
            safeMargin,
            spacing,
            CoachMarkPlacement.Bottom);

        Assert.True(result.ArrowRelativeOffset >= 16.0);
        Assert.True(result.ArrowRelativeOffset <= result.TooltipBounds.Width - 16.0);
    }

    [Fact]
    public void CalculatePlacement_EmptyContainer_ReturnsZeroRectGracefully()
    {
        var result = _engine.CalculatePlacement(
            new Rect(10, 10, 20, 20),
            new Size(100, 50),
            Rect.Zero,
            new Thickness(10),
            10,
            CoachMarkPlacement.Auto);

        Assert.Equal(Rect.Zero, result.TooltipBounds);
    }
}
