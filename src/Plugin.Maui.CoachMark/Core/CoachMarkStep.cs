namespace Plugin.Maui.CoachMark;

/// <summary>
/// Represents an individual step in a guided coach mark tour.
/// </summary>
public sealed class CoachMarkStep
{
    private readonly WeakReference<VisualElement>? _targetWeakRef;
    private readonly Func<VisualElement?>? _targetAccessor;
    private readonly Func<Task<VisualElement?>>? _asyncTargetAccessor;
    private readonly Rect? _staticRect;
    private readonly Func<Rect?>? _rectAccessor;
    private readonly Func<Task<Rect?>>? _asyncRectAccessor;

    /// <summary>
    /// Gets the target resolution kind for this step.
    /// </summary>
    public CoachMarkTargetKind TargetKind { get; }

    /// <summary>
    /// Gets the Shell chrome target descriptor when <see cref="TargetKind"/> is <see cref="CoachMarkTargetKind.ShellElement"/>.
    /// </summary>
    public ShellTarget? ShellTarget { get; }

    /// <summary>
    /// Gets the title of the step.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Gets the description or instruction text of the step.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets or sets the preferred placement for this specific step, overriding global options if specified.
    /// </summary>
    public CoachMarkPlacement? Placement { get; init; }

    /// <summary>
    /// Gets or sets the spotlight shape for this step, overriding global options if specified.
    /// </summary>
    public SpotlightShape? Shape { get; init; }

    /// <summary>
    /// Gets or sets custom spotlight padding for this step.
    /// </summary>
    public Thickness? SpotlightPadding { get; init; }

    /// <summary>
    /// Gets or sets custom corner radius for this step.
    /// </summary>
    public float? SpotlightCornerRadius { get; init; }

    /// <summary>
    /// Gets or sets custom spotlight border color for this step.
    /// </summary>
    public Color? SpotlightBorderColor { get; init; }

    /// <summary>
    /// Gets or sets custom spotlight border width for this step.
    /// </summary>
    public float? SpotlightBorderWidth { get; init; }

    /// <summary>
    /// Gets or sets a custom DataTemplate to render the tooltip content for this step.
    /// </summary>
    public DataTemplate? CustomTemplate { get; init; }

    /// <summary>
    /// Gets or sets an optional custom binding context for the tooltip view.
    /// </summary>
    public object? BindingContext { get; init; }

    /// <summary>
    /// Gets or sets whether touch passes through to the target control for this step.
    /// </summary>
    public bool? AllowTargetInteraction { get; init; }

    /// <summary>
    /// Gets or sets whether tapping outside the spotlight dismisses the tour on this step.
    /// </summary>
    public bool? DismissOnOutsideTap { get; init; }

    /// <summary>
    /// Gets or sets whether tapping the spotlight advances to the next step.
    /// </summary>
    public bool? AdvanceOnTargetTap { get; init; }

    /// <summary>
    /// An asynchronous hook executed immediately before this step is presented.
    /// Useful for scrolling to an element, selecting a tab, or waiting for UI hydration.
    /// Return false to skip this step.
    /// </summary>
    public Func<Task<bool>>? BeforeShowAsync { get; init; }

    /// <summary>
    /// An asynchronous hook executed immediately after this step is dismissed or advanced.
    /// </summary>
    public Func<Task>? AfterDismissAsync { get; init; }

    /// <summary>
    /// Creates a new coach mark step referencing a static VisualElement target.
    /// </summary>
    public CoachMarkStep(VisualElement target, string title, string description)
    {
        ArgumentNullException.ThrowIfNull(target);
        TargetKind = CoachMarkTargetKind.VisualElement;
        _targetWeakRef = new WeakReference<VisualElement>(target);
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Creates a new coach mark step with a dynamic target accessor function.
    /// </summary>
    public CoachMarkStep(Func<VisualElement?> targetAccessor, string title, string description)
    {
        TargetKind = CoachMarkTargetKind.VisualElement;
        _targetAccessor = targetAccessor ?? throw new ArgumentNullException(nameof(targetAccessor));
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Creates a new coach mark step with an asynchronous target accessor function.
    /// </summary>
    public CoachMarkStep(Func<Task<VisualElement?>> asyncTargetAccessor, string title, string description)
    {
        TargetKind = CoachMarkTargetKind.VisualElement;
        _asyncTargetAccessor = asyncTargetAccessor ?? throw new ArgumentNullException(nameof(asyncTargetAccessor));
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Creates a new coach mark step referencing an absolute rectangle in the overlay coordinate space.
    /// </summary>
    public CoachMarkStep(Rect targetRect, string title, string description)
    {
        TargetKind = CoachMarkTargetKind.AbsoluteRect;
        _staticRect = targetRect;
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Creates a new coach mark step referencing a dynamically resolved absolute rectangle in the overlay coordinate space.
    /// </summary>
    public CoachMarkStep(Func<Rect?> rectAccessor, string title, string description)
    {
        TargetKind = CoachMarkTargetKind.AbsoluteRect;
        _rectAccessor = rectAccessor ?? throw new ArgumentNullException(nameof(rectAccessor));
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Creates a new coach mark step referencing an asynchronously resolved absolute rectangle in the overlay coordinate space.
    /// </summary>
    public CoachMarkStep(Func<Task<Rect?>> asyncRectAccessor, string title, string description)
    {
        TargetKind = CoachMarkTargetKind.AbsoluteRect;
        _asyncRectAccessor = asyncRectAccessor ?? throw new ArgumentNullException(nameof(asyncRectAccessor));
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Creates a new coach mark step referencing a rectangle relative to a VisualElement (such as a GraphicsView drawable region).
    /// </summary>
    public CoachMarkStep(VisualElement relativeTo, Rect targetRect, string title, string description)
    {
        ArgumentNullException.ThrowIfNull(relativeTo);
        TargetKind = CoachMarkTargetKind.RelativeRect;
        _targetWeakRef = new WeakReference<VisualElement>(relativeTo);
        _staticRect = targetRect;
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Creates a new coach mark step referencing a dynamically resolved rectangle relative to a VisualElement.
    /// </summary>
    public CoachMarkStep(VisualElement relativeTo, Func<Rect?> rectAccessor, string title, string description)
    {
        ArgumentNullException.ThrowIfNull(relativeTo);
        _rectAccessor = rectAccessor ?? throw new ArgumentNullException(nameof(rectAccessor));
        TargetKind = CoachMarkTargetKind.RelativeRect;
        _targetWeakRef = new WeakReference<VisualElement>(relativeTo);
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Creates a new coach mark step referencing an asynchronously resolved rectangle relative to a VisualElement.
    /// </summary>
    public CoachMarkStep(VisualElement relativeTo, Func<Task<Rect?>> asyncRectAccessor, string title, string description)
    {
        ArgumentNullException.ThrowIfNull(relativeTo);
        _asyncRectAccessor = asyncRectAccessor ?? throw new ArgumentNullException(nameof(asyncRectAccessor));
        TargetKind = CoachMarkTargetKind.RelativeRect;
        _targetWeakRef = new WeakReference<VisualElement>(relativeTo);
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Creates a new coach mark step referencing a native Shell chrome element (such as the flyout icon or a tab bar item).
    /// </summary>
    public CoachMarkStep(ShellTarget shellTarget, string title, string description)
    {
        ShellTarget = shellTarget ?? throw new ArgumentNullException(nameof(shellTarget));
        TargetKind = CoachMarkTargetKind.ShellElement;
        Title = title ?? string.Empty;
        Description = description ?? string.Empty;
    }

    /// <summary>
    /// Resolves the current target element safely. Returns null if the target has been collected or not found.
    /// </summary>
    public async Task<VisualElement?> ResolveTargetAsync()
    {
        if (_asyncTargetAccessor != null)
        {
            return await _asyncTargetAccessor().ConfigureAwait(false);
        }

        if (_targetAccessor != null)
        {
            return _targetAccessor();
        }

        if (_targetWeakRef != null && _targetWeakRef.TryGetTarget(out var target))
        {
            return target;
        }

        return null;
    }

    /// <summary>
    /// Resolves the current target rectangle safely. Returns null if unresolved or if dimensions are non-positive or non-finite.
    /// </summary>
    public async Task<Rect?> ResolveTargetRectAsync()
    {
        Rect? rect = null;

        if (_asyncRectAccessor != null)
        {
            rect = await _asyncRectAccessor().ConfigureAwait(false);
        }
        else if (_rectAccessor != null)
        {
            rect = _rectAccessor();
        }
        else if (_staticRect.HasValue)
        {
            rect = _staticRect.Value;
        }

        if (!rect.HasValue || !IsValidRect(rect.Value))
        {
            return null;
        }

        return rect.Value;
    }

    private static bool IsValidRect(Rect rect)
    {
        return double.IsFinite(rect.X) &&
               double.IsFinite(rect.Y) &&
               double.IsFinite(rect.Width) &&
               double.IsFinite(rect.Height) &&
               rect.Width > 0 &&
               rect.Height > 0;
    }
}
