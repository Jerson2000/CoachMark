namespace Plugin.Maui.CoachMark;

/// <summary>
/// Defines a deterministic placement calculator for positioning tooltips relative to spotlight targets.
/// </summary>
public interface ITooltipPlacementEngine
{
    /// <summary>
    /// Calculates the optimal layout bounds and anchor point for a tooltip card.
    /// </summary>
    PlacementResult CalculatePlacement(
        Rect targetBounds,
        Size tooltipSize,
        Rect containerBounds,
        Thickness safeMargin,
        double spacing,
        CoachMarkPlacement preferredPlacement);
}
