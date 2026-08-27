using Plugin.Maui.CoachMark;

namespace Plugin.Maui.CoachMark.Tests;

public class CoachMarkOptionsTests
{
    [Fact]
    public void Options_DefaultValues_AreSensibleAndSafe()
    {
        var options = new CoachMarkOptions();

        Assert.Equal(0.72f, options.OverlayOpacity);
        Assert.Equal(8f, options.SpotlightCornerRadius);
        Assert.Equal(new Thickness(8), options.SpotlightPadding);
        Assert.Equal(SpotlightShape.RoundedRectangle, options.DefaultSpotlightShape);
        Assert.Equal(CoachMarkPlacement.Auto, options.PreferredPlacement);
        Assert.False(options.AllowTargetInteraction);
        Assert.False(options.DismissOnOutsideTap);
        Assert.True(options.ShowStepIndicator);
        Assert.True(options.EnableAnimations);
        Assert.Equal(TimeSpan.FromMilliseconds(250), options.AnimationDuration);
        Assert.Equal("Next", options.NextButtonText);
        Assert.Equal("Previous", options.PreviousButtonText);
        Assert.Equal("Skip", options.SkipButtonText);
        Assert.Equal("Got it", options.FinishButtonText);
    }

    [Fact]
    public void Options_CustomProperties_AreRetained()
    {
        var options = new CoachMarkOptions
        {
            OverlayOpacity = 0.5f,
            SpotlightCornerRadius = 16f,
            AllowTargetInteraction = true,
            DismissOnOutsideTap = true,
            NextButtonText = "Forward",
            FinishButtonText = "Done"
        };

        Assert.Equal(0.5f, options.OverlayOpacity);
        Assert.Equal(16f, options.SpotlightCornerRadius);
        Assert.True(options.AllowTargetInteraction);
        Assert.True(options.DismissOnOutsideTap);
        Assert.Equal("Forward", options.NextButtonText);
        Assert.Equal("Done", options.FinishButtonText);
    }
}
