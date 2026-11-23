using Microsoft.Maui.Graphics;

namespace Plugin.Maui.CoachMark;

/// <summary>
/// Root overlay layout hosting the GraphicsView spotlight canvas and the positioned tooltip view.
/// </summary>
public sealed class CoachMarkOverlayView : Grid
{
    private readonly GraphicsView _graphicsView;
    private readonly CoachMarkDrawable _drawable;
    private readonly AbsoluteLayout _tooltipLayout;
    private readonly ContentView _tooltipHost;
    private readonly DefaultTooltipView _defaultTooltipView;

    private CoachMarkStep? _currentStep;
    private CoachMarkOptions? _currentOptions;
    private Rect _currentSpotlightRect;

    internal Size? HostViewportSize { get; set; }
    internal Rect CurrentTooltipBounds { get; private set; }

    public event EventHandler? OutsideTapped;
    public event EventHandler? SpotlightTapped;
    public event EventHandler? PreviousRequested;
    public event EventHandler? NextRequested;
    public event EventHandler? SkipRequested;

    public CoachMarkOverlayView()
    {
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        InputTransparent = false;

        _drawable = new CoachMarkDrawable();
        _graphicsView = new GraphicsView
        {
            Drawable = _drawable,
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = false
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnOverlayTapped;
        _graphicsView.GestureRecognizers.Add(tapGesture);

        _tooltipHost = new ContentView
        {
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            InputTransparent = false
        };

        _defaultTooltipView = new DefaultTooltipView();
        _defaultTooltipView.PreviousClicked += (s, e) => PreviousRequested?.Invoke(this, EventArgs.Empty);
        _defaultTooltipView.NextClicked += (s, e) => NextRequested?.Invoke(this, EventArgs.Empty);
        _defaultTooltipView.SkipClicked += (s, e) => SkipRequested?.Invoke(this, EventArgs.Empty);

        _tooltipLayout = new AbsoluteLayout
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = true,
            CascadeInputTransparent = false
        };

        _tooltipLayout.Children.Add(_tooltipHost);

        Children.Add(_graphicsView);
        Children.Add(_tooltipLayout);
    }

    public void ApplyOptions(CoachMarkOptions options)
    {
        _currentOptions = options;
        _drawable.OverlayColor = options.OverlayColor;
        _drawable.OverlayOpacity = options.OverlayOpacity;
        _drawable.BorderColor = options.SpotlightBorderColor;
        _drawable.BorderWidth = options.SpotlightBorderWidth;
        _drawable.BorderDashPattern = options.SpotlightBorderDashPattern;
        _drawable.CornerRadius = options.SpotlightCornerRadius;
        _drawable.Shape = options.DefaultSpotlightShape;
    }

    public async Task TransitionToStepAsync(
        CoachMarkStep step,
        int currentStepIndex,
        int totalSteps,
        Rect targetBounds,
        CoachMarkOptions options,
        ITooltipPlacementEngine placementEngine,
        CancellationToken cancellationToken)
    {
        _currentStep = step;
        _currentOptions = options;

        // Configure step-specific shapes & borders
        _drawable.Shape = step.Shape ?? options.DefaultSpotlightShape;
        _drawable.CornerRadius = step.SpotlightCornerRadius ?? options.SpotlightCornerRadius;
        _drawable.BorderColor = step.SpotlightBorderColor ?? options.SpotlightBorderColor;
        _drawable.BorderWidth = step.SpotlightBorderWidth ?? options.SpotlightBorderWidth;

        var padding = step.SpotlightPadding ?? options.SpotlightPadding;
        var paddedTargetBounds = new Rect(
            targetBounds.X - padding.Left,
            targetBounds.Y - padding.Top,
            targetBounds.Width + padding.HorizontalThickness,
            targetBounds.Height + padding.VerticalThickness);

        // Bind tooltip content
        BindTooltipContent(step, currentStepIndex, totalSteps, options);

        // Ensure overlay bounds are valid when hosted at the platform Window root
        var containerBounds = GetContainerBounds();
        if (containerBounds.Width > 0 && containerBounds.Height > 0 && (Width <= 0 || Height <= 0))
        {
            ((IView)this).Measure(containerBounds.Width, containerBounds.Height);
            ((IView)this).Arrange(containerBounds);
        }

        // Measure tooltip size
        var measuredTooltipSize = MeasureTooltip(containerBounds.Width, options);

        // Calculate placement
        var preferredPlacement = step.Placement ?? options.PreferredPlacement;
        var placement = placementEngine.CalculatePlacement(
            paddedTargetBounds,
            measuredTooltipSize,
            containerBounds,
            options.SafePadding,
            options.TooltipSpacing,
            preferredPlacement);

        // Perform animation or immediate snap
        if (options.EnableAnimations && options.AnimationDuration > TimeSpan.Zero && _currentSpotlightRect.Width > 0)
        {
            await AnimateSpotlightAndTooltipAsync(paddedTargetBounds, placement.TooltipBounds, options.AnimationDuration, options.AnimationEasing, cancellationToken).ConfigureAwait(true);
        }
        else
        {
            _currentSpotlightRect = paddedTargetBounds;
            _drawable.SpotlightRect = new RectF((float)paddedTargetBounds.X, (float)paddedTargetBounds.Y, (float)paddedTargetBounds.Width, (float)paddedTargetBounds.Height);
            _graphicsView.Invalidate();

            PositionTooltip(placement.TooltipBounds);
            _tooltipHost.Opacity = 1.0;
        }
    }

    public void UpdateTargetPosition(
        Rect targetBounds,
        CoachMarkOptions options,
        ITooltipPlacementEngine placementEngine)
    {
        if (_currentStep == null)
        {
            return;
        }

        var padding = _currentStep.SpotlightPadding ?? options.SpotlightPadding;
        var paddedTargetBounds = new Rect(
            targetBounds.X - padding.Left,
            targetBounds.Y - padding.Top,
            targetBounds.Width + padding.HorizontalThickness,
            targetBounds.Height + padding.VerticalThickness);

        _currentSpotlightRect = paddedTargetBounds;
        _drawable.SpotlightRect = new RectF((float)paddedTargetBounds.X, (float)paddedTargetBounds.Y, (float)paddedTargetBounds.Width, (float)paddedTargetBounds.Height);
        _graphicsView.Invalidate();

        var containerBounds = GetContainerBounds();
        var measuredTooltipSize = (_tooltipHost.Width > 0 && _tooltipHost.Height > 0)
            ? new Size(_tooltipHost.Width, _tooltipHost.Height)
            : MeasureTooltip(containerBounds.Width, options);
        var preferredPlacement = _currentStep.Placement ?? options.PreferredPlacement;

        var placement = placementEngine.CalculatePlacement(
            paddedTargetBounds,
            measuredTooltipSize,
            containerBounds,
            options.SafePadding,
            options.TooltipSpacing,
            preferredPlacement);

        PositionTooltip(placement.TooltipBounds);
    }

    internal Rect GetContainerBounds()
    {
        double w = Width > 0 ? Width : (HostViewportSize?.Width ?? 0);
        double h = Height > 0 ? Height : (HostViewportSize?.Height ?? 0);
        return new Rect(0, 0, Math.Max(0, w), Math.Max(0, h));
    }

    private Size MeasureTooltip(double containerWidth, CoachMarkOptions options)
    {
        double effectiveContainerWidth = containerWidth > 0 ? containerWidth : 360;
        double maxAvailableWidth = Math.Max(100, effectiveContainerWidth - options.SafePadding.HorizontalThickness);
        double maxTooltipWidth = Math.Min(360, maxAvailableWidth);

        var measured = _tooltipHost.Measure(maxTooltipWidth, double.PositiveInfinity);
        double w = (measured.Width > 0 && double.IsFinite(measured.Width))
            ? Math.Min(measured.Width, maxAvailableWidth)
            : Math.Min(320, maxTooltipWidth);
        double h = (measured.Height > 0 && double.IsFinite(measured.Height))
            ? measured.Height
            : 180;

        return new Size(w, h);
    }

    private void BindTooltipContent(
        CoachMarkStep step,
        int currentStepIndex,
        int totalSteps,
        CoachMarkOptions options)
    {
        var customTemplate = step.CustomTemplate ?? options.TooltipTemplate;

        if (customTemplate != null)
        {
            var customView = customTemplate.CreateContent() as View;
            if (customView != null)
            {
                customView.BindingContext = step.BindingContext ?? step;
                _tooltipHost.Content = customView;
                return;
            }
        }

        _defaultTooltipView.BindStep(step, currentStepIndex, totalSteps, options);
        _tooltipHost.Content = _defaultTooltipView;
    }

    private void PositionTooltip(Rect bounds)
    {
        CurrentTooltipBounds = bounds;
        AbsoluteLayout.SetLayoutBounds(_tooltipHost, bounds);
        AbsoluteLayout.SetLayoutFlags(_tooltipHost, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
        ((IView)_tooltipLayout).InvalidateMeasure();

        var containerBounds = GetContainerBounds();
        if (containerBounds.Width > 0 && containerBounds.Height > 0)
        {
            ((IView)_tooltipLayout).Measure(containerBounds.Width, containerBounds.Height);
            ((IView)_tooltipLayout).Arrange(containerBounds);
        }
    }

    private async Task AnimateSpotlightAndTooltipAsync(
        Rect targetSpotlightRect,
        Rect targetTooltipBounds,
        TimeSpan duration,
        Easing easing,
        CancellationToken cancellationToken)
    {
        var startSpotlight = _currentSpotlightRect;
        var tcs = new TaskCompletionSource<bool>();

        using var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

        // Fade out tooltip slightly during movement
        await _tooltipHost.FadeToAsync(0.2, (uint)(duration.TotalMilliseconds / 3.0), Easing.Linear);
        if (cancellationToken.IsCancellationRequested)
        {
            tcs.TrySetCanceled(cancellationToken);
            return;
        }

        var animation = new Animation(v =>
        {
            double curX = startSpotlight.X + (targetSpotlightRect.X - startSpotlight.X) * v;
            double curY = startSpotlight.Y + (targetSpotlightRect.Y - startSpotlight.Y) * v;
            double curW = startSpotlight.Width + (targetSpotlightRect.Width - startSpotlight.Width) * v;
            double curH = startSpotlight.Height + (targetSpotlightRect.Height - startSpotlight.Height) * v;

            _drawable.SpotlightRect = new RectF((float)curX, (float)curY, (float)curW, (float)curH);
            _graphicsView.Invalidate();
        });

        animation.Commit(
            this,
            "SpotlightTransition",
            length: (uint)duration.TotalMilliseconds,
            easing: easing,
            finished: (v, c) =>
            {
                _currentSpotlightRect = targetSpotlightRect;
                _drawable.SpotlightRect = new RectF((float)targetSpotlightRect.X, (float)targetSpotlightRect.Y, (float)targetSpotlightRect.Width, (float)targetSpotlightRect.Height);
                _graphicsView.Invalidate();
                tcs.TrySetResult(true);
            });

        try
        {
            await Task.WhenAny(tcs.Task, Task.Delay(duration + TimeSpan.FromMilliseconds(250), cancellationToken)).ConfigureAwait(true);
            if (!tcs.Task.IsCompleted)
            {
                this.AbortAnimation("SpotlightTransition");
                _currentSpotlightRect = targetSpotlightRect;
                _drawable.SpotlightRect = new RectF((float)targetSpotlightRect.X, (float)targetSpotlightRect.Y, (float)targetSpotlightRect.Width, (float)targetSpotlightRect.Height);
                _graphicsView.Invalidate();
            }

            PositionTooltip(targetTooltipBounds);
            await _tooltipHost.FadeToAsync(1.0, (uint)(duration.TotalMilliseconds / 2.0), Easing.Linear);
        }
        catch (OperationCanceledException)
        {
            this.AbortAnimation("SpotlightTransition");
            _currentSpotlightRect = targetSpotlightRect;
            _drawable.SpotlightRect = new RectF((float)targetSpotlightRect.X, (float)targetSpotlightRect.Y, (float)targetSpotlightRect.Width, (float)targetSpotlightRect.Height);
            _graphicsView.Invalidate();
            PositionTooltip(targetTooltipBounds);
            _tooltipHost.Opacity = 1.0;
        }
    }

    private void OnOverlayTapped(object? sender, TappedEventArgs e)
    {
        var tapPointNullable = e.GetPosition(_graphicsView);
        if (!tapPointNullable.HasValue)
        {
            OutsideTapped?.Invoke(this, EventArgs.Empty);
            return;
        }

        var tapPoint = tapPointNullable.Value;
        if (_currentSpotlightRect.Contains(tapPoint))
        {
            SpotlightTapped?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            OutsideTapped?.Invoke(this, EventArgs.Empty);
        }
    }
}
