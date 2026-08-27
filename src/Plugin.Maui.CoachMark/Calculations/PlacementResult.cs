namespace Plugin.Maui.CoachMark;

/// <summary>
/// Contains the calculated geometric positioning result for a coach mark tooltip.
/// </summary>
/// <param name="TooltipBounds">The absolute layout rectangle for the tooltip within the overlay coordinate space.</param>
/// <param name="EffectivePlacement">The effective placement chosen after evaluating container constraints.</param>
/// <param name="TargetAnchorPoint">The point on the target spotlight that the tooltip arrow should point towards.</param>
/// <param name="ArrowRelativeOffset">The relative offset (in pixels) where the arrow should be anchored.</param>
public readonly record struct PlacementResult(
    Rect TooltipBounds,
    CoachMarkPlacement EffectivePlacement,
    Point TargetAnchorPoint,
    double ArrowRelativeOffset);

