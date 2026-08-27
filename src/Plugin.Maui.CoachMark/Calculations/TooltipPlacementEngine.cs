namespace Plugin.Maui.CoachMark;

/// <summary>
/// Deterministic engine that computes tooltip placement, collision prevention, safe area clamping, and arrow offsets.
/// </summary>
public sealed class TooltipPlacementEngine : ITooltipPlacementEngine
{
    private const double MinArrowMargin = 16.0;

    public PlacementResult CalculatePlacement(
        Rect targetBounds,
        Size tooltipSize,
        Rect containerBounds,
        Thickness safeMargin,
        double spacing,
        CoachMarkPlacement preferredPlacement)
    {
        if (containerBounds.Width <= 0 || containerBounds.Height <= 0)
        {
            return new PlacementResult(Rect.Zero, CoachMarkPlacement.Center, Point.Zero, 0);
        }

        // Apply safe viewport boundaries
        double usableLeft = containerBounds.Left + safeMargin.Left;
        double usableTop = containerBounds.Top + safeMargin.Top;
        double usableRight = Math.Max(usableLeft, containerBounds.Right - safeMargin.Right);
        double usableBottom = Math.Max(usableTop, containerBounds.Bottom - safeMargin.Bottom);

        double usableWidth = Math.Max(0, usableRight - usableLeft);
        double usableHeight = Math.Max(0, usableBottom - usableTop);

        // Clamp tooltip size if larger than entire usable viewport
        double boundedTooltipWidth = Math.Min(tooltipSize.Width, usableWidth);
        double boundedTooltipHeight = Math.Min(tooltipSize.Height, usableHeight);

        // Calculate available space in all 4 cardinal directions
        double spaceTop = targetBounds.Top - spacing - usableTop;
        double spaceBottom = usableBottom - (targetBounds.Bottom + spacing);
        double spaceLeft = targetBounds.Left - spacing - usableLeft;
        double spaceRight = usableRight - (targetBounds.Right + spacing);

        CoachMarkPlacement chosenPlacement = ResolvePlacement(
            preferredPlacement,
            boundedTooltipWidth,
            boundedTooltipHeight,
            spaceTop,
            spaceBottom,
            spaceLeft,
            spaceRight);

        double tooltipX;
        double tooltipY;
        Point targetAnchor;
        double arrowOffset;

        double targetCenterX = targetBounds.X + (targetBounds.Width / 2.0);
        double targetCenterY = targetBounds.Y + (targetBounds.Height / 2.0);

        switch (chosenPlacement)
        {
            case CoachMarkPlacement.Bottom:
                tooltipY = targetBounds.Bottom + spacing;
                tooltipX = targetCenterX - (boundedTooltipWidth / 2.0);
                targetAnchor = new Point(targetCenterX, targetBounds.Bottom);
                break;

            case CoachMarkPlacement.Top:
                tooltipY = targetBounds.Top - spacing - boundedTooltipHeight;
                tooltipX = targetCenterX - (boundedTooltipWidth / 2.0);
                targetAnchor = new Point(targetCenterX, targetBounds.Top);
                break;

            case CoachMarkPlacement.Right:
                tooltipX = targetBounds.Right + spacing;
                tooltipY = targetCenterY - (boundedTooltipHeight / 2.0);
                targetAnchor = new Point(targetBounds.Right, targetCenterY);
                break;

            case CoachMarkPlacement.Left:
                tooltipX = targetBounds.Left - spacing - boundedTooltipWidth;
                tooltipY = targetCenterY - (boundedTooltipHeight / 2.0);
                targetAnchor = new Point(targetBounds.Left, targetCenterY);
                break;

            case CoachMarkPlacement.Center:
            default:
                tooltipX = usableLeft + (usableWidth - boundedTooltipWidth) / 2.0;
                tooltipY = usableTop + (usableHeight - boundedTooltipHeight) / 2.0;
                targetAnchor = new Point(targetCenterX, targetCenterY);
                break;
        }

        // Clamp tooltip within safe container bounds
        tooltipX = Math.Clamp(tooltipX, usableLeft, Math.Max(usableLeft, usableRight - boundedTooltipWidth));
        tooltipY = Math.Clamp(tooltipY, usableTop, Math.Max(usableTop, usableBottom - boundedTooltipHeight));

        // Calculate relative arrow offset along the tooltip edge
        if (chosenPlacement == CoachMarkPlacement.Top || chosenPlacement == CoachMarkPlacement.Bottom)
        {
            double relativeTargetX = targetCenterX - tooltipX;
            arrowOffset = Math.Clamp(
                relativeTargetX,
                MinArrowMargin,
                Math.Max(MinArrowMargin, boundedTooltipWidth - MinArrowMargin));
        }
        else if (chosenPlacement == CoachMarkPlacement.Left || chosenPlacement == CoachMarkPlacement.Right)
        {
            double relativeTargetY = targetCenterY - tooltipY;
            arrowOffset = Math.Clamp(
                relativeTargetY,
                MinArrowMargin,
                Math.Max(MinArrowMargin, boundedTooltipHeight - MinArrowMargin));
        }
        else
        {
            arrowOffset = boundedTooltipWidth / 2.0;
        }

        var finalBounds = new Rect(tooltipX, tooltipY, boundedTooltipWidth, boundedTooltipHeight);
        return new PlacementResult(finalBounds, chosenPlacement, targetAnchor, arrowOffset);
    }

    private static CoachMarkPlacement ResolvePlacement(
        CoachMarkPlacement preferred,
        double width,
        double height,
        double spaceTop,
        double spaceBottom,
        double spaceLeft,
        double spaceRight)
    {
        if (preferred == CoachMarkPlacement.Center)
        {
            return CoachMarkPlacement.Center;
        }

        // Check if preferred placement fits completely
        if (preferred == CoachMarkPlacement.Bottom && spaceBottom >= height)
        {
            return CoachMarkPlacement.Bottom;
        }

        if (preferred == CoachMarkPlacement.Top && spaceTop >= height)
        {
            return CoachMarkPlacement.Top;
        }

        if (preferred == CoachMarkPlacement.Right && spaceRight >= width)
        {
            return CoachMarkPlacement.Right;
        }

        if (preferred == CoachMarkPlacement.Left && spaceLeft >= width)
        {
            return CoachMarkPlacement.Left;
        }

        // If preferred doesn't fit (or was Auto), evaluate candidates in priority order: Bottom -> Top -> Right -> Left
        if (spaceBottom >= height)
        {
            return CoachMarkPlacement.Bottom;
        }

        if (spaceTop >= height)
        {
            return CoachMarkPlacement.Top;
        }

        if (spaceRight >= width)
        {
            return CoachMarkPlacement.Right;
        }

        if (spaceLeft >= width)
        {
            return CoachMarkPlacement.Left;
        }

        // Fallback: pick the vertical placement that offers the largest available space
        if (spaceBottom >= spaceTop && spaceBottom >= spaceRight && spaceBottom >= spaceLeft)
        {
            return CoachMarkPlacement.Bottom;
        }

        if (spaceTop >= spaceBottom && spaceTop >= spaceRight && spaceTop >= spaceLeft)
        {
            return CoachMarkPlacement.Top;
        }

        if (spaceRight >= spaceLeft)
        {
            return CoachMarkPlacement.Right;
        }

        return CoachMarkPlacement.Left;
    }
}
