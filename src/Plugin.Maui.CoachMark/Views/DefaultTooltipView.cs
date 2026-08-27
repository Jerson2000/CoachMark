using Microsoft.Maui.Controls.Shapes;

namespace Plugin.Maui.CoachMark;

/// <summary>
/// Default accessible, responsive tooltip card view for displaying coach mark steps.
/// </summary>
public sealed class DefaultTooltipView : ContentView
{
    private readonly Border _cardBorder;
    private readonly Label _stepBadge;
    private readonly Label _titleLabel;
    private readonly Label _descriptionLabel;
    private readonly Button _previousButton;
    private readonly Button _nextButton;
    private readonly Button _skipButton;
    private readonly Button _closeButton;
    private readonly Grid _headerGrid;

    public event EventHandler? PreviousClicked;
    public event EventHandler? NextClicked;
    public event EventHandler? SkipClicked;

    public DefaultTooltipView()
    {
        InputTransparent = false;

        _stepBadge = new Label
        {
            FontSize = 11,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            AutomationId = "CoachMarkStepBadge"
        };

        _closeButton = new Button
        {
            Text = "✕",
            FontSize = 14,
            Padding = new Thickness(0),
            WidthRequest = 28,
            HeightRequest = 28,
            CornerRadius = 14,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#6B7280"),
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center,
            AutomationId = "CoachMarkCloseButton"
        };
        SemanticProperties.SetDescription(_closeButton, "Close tutorial");
        _closeButton.Clicked += (s, e) => SkipClicked?.Invoke(this, EventArgs.Empty);

        _headerGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }
            }
        };
        _headerGrid.Add(_stepBadge, 0, 0);
        _headerGrid.Add(_closeButton, 1, 0);

        _titleLabel = new Label
        {
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 4, 0, 4),
            AutomationId = "CoachMarkTitleLabel"
        };
        SemanticProperties.SetHeadingLevel(_titleLabel, SemanticHeadingLevel.Level1);

        _descriptionLabel = new Label
        {
            FontSize = 13,
            LineHeight = 1.25,
            Margin = new Thickness(0, 0, 0, 12),
            AutomationId = "CoachMarkDescriptionLabel"
        };

        _previousButton = new Button
        {
            FontSize = 13,
            Padding = new Thickness(12, 6),
            HeightRequest = 36,
            CornerRadius = 6,
            BackgroundColor = Colors.Transparent,
            AutomationId = "CoachMarkPreviousButton"
        };
        SemanticProperties.SetDescription(_previousButton, "Go to previous step");
        _previousButton.Clicked += (s, e) => PreviousClicked?.Invoke(this, EventArgs.Empty);

        _nextButton = new Button
        {
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            Padding = new Thickness(16, 6),
            HeightRequest = 36,
            CornerRadius = 6,
            TextColor = Colors.White,
            AutomationId = "CoachMarkNextButton"
        };
        SemanticProperties.SetDescription(_nextButton, "Advance to next step");
        _nextButton.Clicked += (s, e) => NextClicked?.Invoke(this, EventArgs.Empty);

        _skipButton = new Button
        {
            FontSize = 13,
            Padding = new Thickness(8, 6),
            HeightRequest = 36,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#6B7280"),
            HorizontalOptions = LayoutOptions.Start,
            AutomationId = "CoachMarkSkipButton"
        };
        SemanticProperties.SetDescription(_skipButton, "Skip tutorial tour");
        _skipButton.Clicked += (s, e) => SkipClicked?.Invoke(this, EventArgs.Empty);

        var actionsLayout = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Auto }
            },
            ColumnSpacing = 8
        };

        actionsLayout.Add(_skipButton, 0, 0);
        actionsLayout.Add(_previousButton, 2, 0);
        actionsLayout.Add(_nextButton, 3, 0);

        var contentStack = new VerticalStackLayout
        {
            Spacing = 4,
            Children =
            {
                _headerGrid,
                _titleLabel,
                _descriptionLabel,
                actionsLayout
            }
        };

        _cardBorder = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            StrokeThickness = 1,
            Stroke = Color.FromArgb("#E5E7EB"),
            Shadow = new Shadow
            {
                Brush = Brush.Black,
                Offset = new Point(0, 4),
                Radius = 12,
                Opacity = 0.2f
            },
            Padding = new Thickness(16),
            Content = contentStack
        };

        Content = _cardBorder;
    }

    public void BindStep(
        CoachMarkStep step,
        int currentStepIndex,
        int totalSteps,
        CoachMarkOptions options)
    {
        _titleLabel.Text = step.Title;
        _titleLabel.TextColor = options.TitleTextColor;

        _descriptionLabel.Text = step.Description;
        _descriptionLabel.TextColor = options.DescriptionTextColor;

        _cardBorder.BackgroundColor = options.CardBackgroundColor;

        bool hasMultipleSteps = totalSteps > 1;
        _headerGrid.IsVisible = options.ShowStepIndicator || hasMultipleSteps;
        _stepBadge.IsVisible = options.ShowStepIndicator && hasMultipleSteps;
        _stepBadge.Text = $"STEP {currentStepIndex + 1} OF {totalSteps}";
        _stepBadge.TextColor = options.AccentColor;

        bool isFirstStep = currentStepIndex == 0;
        bool isLastStep = currentStepIndex == totalSteps - 1;

        _previousButton.IsVisible = !isFirstStep && hasMultipleSteps;
        _previousButton.Text = options.PreviousButtonText;
        _previousButton.TextColor = options.AccentColor;

        _skipButton.IsVisible = hasMultipleSteps && !isLastStep;
        _skipButton.Text = options.SkipButtonText;

        _nextButton.Text = isLastStep ? options.FinishButtonText : options.NextButtonText;
        _nextButton.BackgroundColor = options.AccentColor;

        // Accessibility announcement
        string accessibilityText = $"{step.Title}. {step.Description}";
        if (hasMultipleSteps)
        {
            accessibilityText = $"Step {currentStepIndex + 1} of {totalSteps}. " + accessibilityText;
        }
        SemanticScreenReader.Default.Announce(accessibilityText);
    }
}
