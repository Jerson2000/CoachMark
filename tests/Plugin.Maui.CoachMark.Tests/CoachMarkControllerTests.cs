using Microsoft.Maui.Controls;
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
}
