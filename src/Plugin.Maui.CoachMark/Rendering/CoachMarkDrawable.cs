using Microsoft.Maui.Graphics;

namespace Plugin.Maui.CoachMark;

/// <summary>
/// High-performance IDrawable that renders a dimmed overlay with a transparent hardware-accelerated cutout hole
/// and an optional styled focus ring/border.
/// </summary>
public sealed class CoachMarkDrawable : IDrawable
{
    private readonly object _syncLock = new();

    public RectF SpotlightRect { get; set; }
    public SpotlightShape Shape { get; set; } = SpotlightShape.RoundedRectangle;
    public float CornerRadius { get; set; } = 8f;
    public Color OverlayColor { get; set; } = Colors.Black;
    public float OverlayOpacity { get; set; } = 0.72f;
    public Color? BorderColor { get; set; }
    public float BorderWidth { get; set; }
    public float[]? BorderDashPattern { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        lock (_syncLock)
        {
            if (dirtyRect.Width <= 0 || dirtyRect.Height <= 0)
            {
                return;
            }

            canvas.SaveState();

            // Setup overlay fill color with opacity
            var fillColor = OverlayColor.WithAlpha(OverlayOpacity);
            canvas.FillColor = fillColor;

            // Build cutout path using EvenOdd winding to punch out the spotlight area
            var path = new PathF();
            path.AppendRectangle(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);

            if (SpotlightRect.Width > 0 && SpotlightRect.Height > 0)
            {
                switch (Shape)
                {
                    case SpotlightShape.Rectangle:
                        path.AppendRectangle(SpotlightRect.X, SpotlightRect.Y, SpotlightRect.Width, SpotlightRect.Height);
                        break;

                    case SpotlightShape.Circle:
                        float diameter = Math.Max(SpotlightRect.Width, SpotlightRect.Height);
                        float cx = SpotlightRect.Center.X;
                        float cy = SpotlightRect.Center.Y;
                        path.AppendEllipse(cx - diameter / 2f, cy - diameter / 2f, diameter, diameter);
                        break;

                    case SpotlightShape.Oval:
                        path.AppendEllipse(SpotlightRect.X, SpotlightRect.Y, SpotlightRect.Width, SpotlightRect.Height);
                        break;

                    case SpotlightShape.RoundedRectangle:
                    default:
                        path.AppendRoundedRectangle(SpotlightRect.X, SpotlightRect.Y, SpotlightRect.Width, SpotlightRect.Height, CornerRadius);
                        break;
                }
            }

            canvas.FillPath(path, WindingMode.EvenOdd);

            // Draw optional spotlight focus border
            if (BorderWidth > 0 && BorderColor != null && SpotlightRect.Width > 0 && SpotlightRect.Height > 0)
            {
                canvas.StrokeColor = BorderColor;
                canvas.StrokeSize = BorderWidth;

                if (BorderDashPattern != null && BorderDashPattern.Length > 0)
                {
                    canvas.StrokeDashPattern = BorderDashPattern;
                }

                switch (Shape)
                {
                    case SpotlightShape.Rectangle:
                        canvas.DrawRectangle(SpotlightRect.X, SpotlightRect.Y, SpotlightRect.Width, SpotlightRect.Height);
                        break;

                    case SpotlightShape.Circle:
                        float diameter = Math.Max(SpotlightRect.Width, SpotlightRect.Height);
                        float cx = SpotlightRect.Center.X;
                        float cy = SpotlightRect.Center.Y;
                        canvas.DrawEllipse(cx - diameter / 2f, cy - diameter / 2f, diameter, diameter);
                        break;

                    case SpotlightShape.Oval:
                        canvas.DrawEllipse(SpotlightRect.X, SpotlightRect.Y, SpotlightRect.Width, SpotlightRect.Height);
                        break;

                    case SpotlightShape.RoundedRectangle:
                    default:
                        canvas.DrawRoundedRectangle(SpotlightRect.X, SpotlightRect.Y, SpotlightRect.Width, SpotlightRect.Height, CornerRadius);
                        break;
                }
            }

            canvas.RestoreState();
        }
    }
}
