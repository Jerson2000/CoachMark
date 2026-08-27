using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Plugin.Maui.CoachMark;

namespace Plugin.Maui.CoachMark.Tests;

#pragma warning disable CS0618
public class VisualTreeCoordinateResolverTests
{
    private readonly VisualTreeCoordinateResolver _resolver = new();

    [Fact]
    public void GetBoundingBox_SingleElement_ReturnsItsBounds()
    {
        var button = new Button
        {
            WidthRequest = 100,
            HeightRequest = 40,
            IsVisible = true
        };
        button.Layout(new Rect(50, 60, 100, 40));

        var bounds = _resolver.GetBoundingBox(button);

        Assert.NotNull(bounds);
        Assert.Equal(new Rect(50, 60, 100, 40), bounds.Value);
    }

    [Fact]
    public void GetBoundingBox_NestedInContainer_AccumulatesParentCoordinates()
    {
        var parentLayout = new Grid();
        parentLayout.Layout(new Rect(20, 30, 300, 300));

        var childButton = new Button
        {
            IsVisible = true
        };
        childButton.Layout(new Rect(10, 15, 80, 40));

        parentLayout.Children.Add(childButton);

        var bounds = _resolver.GetBoundingBox(childButton);

        Assert.NotNull(bounds);
        // Absolute X = 10 + 20 = 30, Absolute Y = 15 + 30 = 45
        Assert.Equal(30, bounds.Value.X);
        Assert.Equal(45, bounds.Value.Y);
        Assert.Equal(80, bounds.Value.Width);
        Assert.Equal(40, bounds.Value.Height);
    }

    [Fact]
    public void GetBoundingBox_WithTranslation_IncludesTranslationValues()
    {
        var button = new Button
        {
            TranslationX = 25,
            TranslationY = -10,
            IsVisible = true
        };
        button.Layout(new Rect(100, 100, 80, 40));

        var bounds = _resolver.GetBoundingBox(button);

        Assert.NotNull(bounds);
        Assert.Equal(125, bounds.Value.X);
        Assert.Equal(90, bounds.Value.Y);
    }

    [Fact]
    public void GetBoundingBox_InvisibleElement_ReturnsNull()
    {
        var button = new Button
        {
            IsVisible = false
        };
        button.Layout(new Rect(100, 100, 80, 40));

        var bounds = _resolver.GetBoundingBox(button);

        Assert.Null(bounds);
    }

    [Fact]
    public void GetBoundingBox_NullElement_ReturnsNull()
    {
        var bounds = _resolver.GetBoundingBox(null!);
        Assert.Null(bounds);
    }

    [Fact]
    public void GetBoundingBox_ZeroHeightElement_ReturnsNull()
    {
        var button = new Button
        {
            IsVisible = true
        };
        button.Layout(new Rect(100, 100, 50, 0));

        var bounds = _resolver.GetBoundingBox(button);

        Assert.Null(bounds);
    }

    [Fact]
    public void GetBoundingBox_InsideScrollView_AdjustsForScrollOffsets()
    {
        var scrollView = new ScrollView
        {
            Orientation = ScrollOrientation.Both
        };
        scrollView.Layout(new Rect(0, 0, 400, 600));

        var container = new VerticalStackLayout();
        container.Layout(new Rect(0, 0, 400, 1200));
        scrollView.Content = container;

        var button = new Button { IsVisible = true };
        button.Layout(new Rect(50, 400, 100, 50));
        container.Children.Add(button);

        // Before scroll
        var boundsBefore = _resolver.GetBoundingBox(button);
        Assert.NotNull(boundsBefore);
        Assert.Equal(50, boundsBefore.Value.X);
        Assert.Equal(400, boundsBefore.Value.Y);
    }

    [Fact]
    public void GetBoundingBox_RelativeToAncestor_StopsCoordinateAccumulationAtRelativeTo()
    {
        var outerGrid = new Grid();
        outerGrid.Layout(new Rect(100, 100, 500, 500));

        var innerGrid = new Grid();
        innerGrid.Layout(new Rect(20, 30, 200, 200));
        outerGrid.Children.Add(innerGrid);

        var button = new Button { IsVisible = true };
        button.Layout(new Rect(10, 15, 80, 40));
        innerGrid.Children.Add(button);

        // Relative to innerGrid: button is at (10, 15)
        var boundsRelativeInner = _resolver.GetBoundingBox(button, relativeTo: innerGrid);
        Assert.NotNull(boundsRelativeInner);
        Assert.Equal(10, boundsRelativeInner.Value.X);
        Assert.Equal(15, boundsRelativeInner.Value.Y);

        // Relative to outerGrid: button is at (10 + 20 = 30, 15 + 30 = 45)
        var boundsRelativeOuter = _resolver.GetBoundingBox(button, relativeTo: outerGrid);
        Assert.NotNull(boundsRelativeOuter);
        Assert.Equal(30, boundsRelativeOuter.Value.X);
        Assert.Equal(45, boundsRelativeOuter.Value.Y);
    }
}
