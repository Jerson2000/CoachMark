namespace Plugin.Maui.CoachMark;

/// <summary>
/// Configuration options for the Coach Mark component.
/// </summary>
public sealed class CoachMarkOptions
{
    /// <summary>
    /// Gets or sets the color of the dimmed overlay. Default is Black.
    /// </summary>
    public Color OverlayColor { get; init; } = Colors.Black;

    /// <summary>
    /// Gets or sets the opacity of the dimmed overlay (0.0 to 1.0). Default is 0.72.
    /// </summary>
    public float OverlayOpacity { get; init; } = 0.72f;

    /// <summary>
    /// Gets or sets the default padding around the target element for the spotlight cutout. Default is 8 on all sides.
    /// </summary>
    public Thickness SpotlightPadding { get; init; } = new Thickness(8);

    /// <summary>
    /// Gets or sets the default corner radius for rounded rectangle spotlights. Default is 8.
    /// </summary>
    public float SpotlightCornerRadius { get; init; } = 8f;

    /// <summary>
    /// Gets or sets the optional border color drawn around the spotlight cutout. Default is null (no border).
    /// </summary>
    public Color? SpotlightBorderColor { get; init; }

    /// <summary>
    /// Gets or sets the border width around the spotlight cutout. Default is 0.
    /// </summary>
    public float SpotlightBorderWidth { get; init; } = 0f;

    /// <summary>
    /// Gets or sets an optional dash pattern for the spotlight border (e.g., new float[] { 4, 4 }).
    /// </summary>
    public float[]? SpotlightBorderDashPattern { get; init; }

    /// <summary>
    /// Gets or sets the default spotlight shape. Default is RoundedRectangle.
    /// </summary>
    public SpotlightShape DefaultSpotlightShape { get; init; } = SpotlightShape.RoundedRectangle;

    /// <summary>
    /// Gets or sets the duration of transitions and animations. Default is 250 milliseconds.
    /// </summary>
    public TimeSpan AnimationDuration { get; init; } = TimeSpan.FromMilliseconds(250);

    /// <summary>
    /// Gets or sets the animation easing function. Default is Easing.CubicInOut.
    /// </summary>
    public Easing AnimationEasing { get; init; } = Easing.CubicInOut;

    /// <summary>
    /// Gets or sets whether animations are enabled. Respects reduced-motion preference when set to false. Default is true.
    /// </summary>
    public bool EnableAnimations { get; init; } = true;

    /// <summary>
    /// Gets or sets the default preferred placement for tooltips. Default is Auto.
    /// </summary>
    public CoachMarkPlacement PreferredPlacement { get; init; } = CoachMarkPlacement.Auto;

    /// <summary>
    /// Controls whether the overlay is hosted inside the active ContentPage or at the platform Window root.
    /// Default is Auto (uses Window hosting when any step targets a Shell element, otherwise Page hosting).
    /// </summary>
    public CoachMarkOverlayHostMode OverlayHostMode { get; init; } = CoachMarkOverlayHostMode.Auto;

    /// <summary>
    /// Gets or sets whether touches on the highlighted spotlight target pass through to the underlying control. Default is false.
    /// </summary>
    public bool AllowTargetInteraction { get; init; } = false;

    /// <summary>
    /// Gets or sets whether tapping outside the spotlight dismisses the tour. Default is false.
    /// </summary>
    public bool DismissOnOutsideTap { get; init; } = false;

    /// <summary>
    /// Gets or sets whether tapping the spotlight advances to the next step. Default is false.
    /// </summary>
    public bool AdvanceOnTargetTap { get; init; } = false;

    /// <summary>
    /// Gets or sets whether to display the step indicator (e.g. "Step 1 of 3"). Default is true.
    /// </summary>
    public bool ShowStepIndicator { get; init; } = true;

    /// <summary>
    /// Gets or sets whether the coach mark should automatically attempt to scroll ancestor ScrollViews to bring the target into view. Default is true.
    /// </summary>
    public bool ScrollToTargetIfNeeded { get; init; } = true;

    /// <summary>
    /// Gets or sets the minimum gap in pixels between the spotlight and the tooltip. Default is 12.
    /// </summary>
    public double TooltipSpacing { get; init; } = 12.0;

    /// <summary>
    /// Gets or sets the safe viewport padding to prevent tooltips from touching the screen edges or safe area. Default is 16 on all sides.
    /// </summary>
    public Thickness SafePadding { get; init; } = new Thickness(16);

    /// <summary>
    /// Gets or sets an optional custom DataTemplate for rendering tooltips.
    /// </summary>
    public DataTemplate? TooltipTemplate { get; init; }

    /// <summary>
    /// Gets or sets the text for the Next button. Default is "Next".
    /// </summary>
    public string NextButtonText { get; init; } = "Next";

    /// <summary>
    /// Gets or sets the text for the Previous button. Default is "Previous".
    /// </summary>
    public string PreviousButtonText { get; init; } = "Previous";

    /// <summary>
    /// Gets or sets the text for the Skip button. Default is "Skip".
    /// </summary>
    public string SkipButtonText { get; init; } = "Skip";

    /// <summary>
    /// Gets or sets the text for the Finish button on the final step. Default is "Got it".
    /// </summary>
    public string FinishButtonText { get; init; } = "Got it";

    /// <summary>
    /// Gets or sets the background color of the default tooltip card. Default is white/surface.
    /// </summary>
    public Color CardBackgroundColor { get; init; } = Colors.White;

    /// <summary>
    /// Gets or sets the title text color of the default tooltip card. Default is dark gray / black.
    /// </summary>
    public Color TitleTextColor { get; init; } = Color.FromArgb("#1F2937");

    /// <summary>
    /// Gets or sets the description text color of the default tooltip card. Default is medium gray.
    /// </summary>
    public Color DescriptionTextColor { get; init; } = Color.FromArgb("#4B5563");

    /// <summary>
    /// Gets or sets the accent color used for primary action buttons and step badges. Default is MAUI primary indigo.
    /// </summary>
    public Color AccentColor { get; init; } = Color.FromArgb("#512BD4");

    /// <summary>
    /// Callback invoked when a step is about to appear.
    /// </summary>
    public Action<CoachMarkStep, int>? OnStepAppearing { get; init; }

    /// <summary>
    /// Callback invoked when a step has disappeared.
    /// </summary>
    public Action<CoachMarkStep, int>? OnStepDisappeared { get; init; }
}
