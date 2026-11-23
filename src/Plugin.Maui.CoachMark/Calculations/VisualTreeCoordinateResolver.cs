namespace Plugin.Maui.CoachMark;

/// <summary>
/// Resolves absolute layout coordinates by leveraging platform-native visual transforms when available
/// (such as for CollectionView, ListView, and nested transformed controls), falling back to walking
/// the VisualElement ancestor hierarchy adjusting for margins, paddings, and scroll offsets.
/// </summary>
public sealed class VisualTreeCoordinateResolver : ICoordinateResolver
{
    public Rect? GetBoundingBox(VisualElement target, VisualElement? relativeTo = null)
    {
        if (target == null || !target.IsVisible)
        {
            return null;
        }

#if WINDOWS
        if (target.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement targetPlatformView)
        {
            if (targetPlatformView.Visibility != Microsoft.UI.Xaml.Visibility.Visible ||
                targetPlatformView.ActualWidth <= 0 ||
                targetPlatformView.ActualHeight <= 0)
            {
                return null;
            }

            Microsoft.UI.Xaml.UIElement? relativePlatformView = relativeTo?.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement;
            if (relativePlatformView == null && targetPlatformView.XamlRoot?.Content != null)
            {
                relativePlatformView = targetPlatformView.XamlRoot.Content;
            }

            if (relativePlatformView != null)
            {
                try
                {
                    var transform = targetPlatformView.TransformToVisual(relativePlatformView);
                    var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                    return new Rect(point.X, point.Y, targetPlatformView.ActualWidth, targetPlatformView.ActualHeight);
                }
                catch
                {
                    // When relativeTo is hosted in a WinUI Popup, TransformToVisual across visual roots fails;
                    // retry relative to XamlRoot.Content before falling back to the MAUI visual tree walk.
                    if (targetPlatformView.XamlRoot?.Content is Microsoft.UI.Xaml.UIElement xamlRootContent &&
                        !ReferenceEquals(relativePlatformView, xamlRootContent))
                    {
                        try
                        {
                            var rootTransform = targetPlatformView.TransformToVisual(xamlRootContent);
                            var point = rootTransform.TransformPoint(new Windows.Foundation.Point(0, 0));
                            return new Rect(point.X, point.Y, targetPlatformView.ActualWidth, targetPlatformView.ActualHeight);
                        }
                        catch
                        {
                            // Fallback to MAUI visual tree walk
                        }
                    }
                }
            }
        }
#elif IOS || MACCATALYST
        if (target.Handler?.PlatformView is UIKit.UIView targetPlatformView)
        {
            if (targetPlatformView.Hidden ||
                targetPlatformView.Bounds.Width <= 0 ||
                targetPlatformView.Bounds.Height <= 0)
            {
                return null;
            }

            var relativePlatformView = relativeTo?.Handler?.PlatformView as UIKit.UIView ?? targetPlatformView.Window;
            if (relativePlatformView != null)
            {
                try
                {
                    var bounds = targetPlatformView.Bounds;
                    var convertedRect = targetPlatformView.ConvertRectToView(bounds, relativePlatformView);
                    return new Rect(convertedRect.X, convertedRect.Y, convertedRect.Width, convertedRect.Height);
                }
                catch
                {
                    // Fallback to MAUI visual tree walk if platform transform fails
                }
            }
        }
#elif ANDROID
        if (target.Handler?.PlatformView is Android.Views.View targetPlatformView)
        {
            if (targetPlatformView.Visibility != Android.Views.ViewStates.Visible ||
                targetPlatformView.Width <= 0 ||
                targetPlatformView.Height <= 0)
            {
                return null;
            }

            try
            {
                int[] targetLocation = new int[2];
                targetPlatformView.GetLocationOnScreen(targetLocation);

                int targetX = targetLocation[0];
                int targetY = targetLocation[1];

                var relativePlatformView = relativeTo?.Handler?.PlatformView as Android.Views.View;
                if (relativePlatformView != null)
                {
                    int[] relativeLocation = new int[2];
                    relativePlatformView.GetLocationOnScreen(relativeLocation);
                    targetX -= relativeLocation[0];
                    targetY -= relativeLocation[1];
                }

                float density = targetPlatformView.Context?.Resources?.DisplayMetrics?.Density ?? 1.0f;
                if (density <= 0f)
                {
                    density = 1.0f;
                }

                double x = targetX / density;
                double y = targetY / density;
                double width = targetPlatformView.Width / density;
                double height = targetPlatformView.Height / density;

                return new Rect(x, y, width, height);
            }
            catch
            {
                // Fallback to MAUI visual tree walk if platform measurement fails
            }
        }
#endif

        if (target.Width <= 0 || target.Height <= 0)
        {
            return null;
        }

        double fallbackWidth = target.Width;
        double fallbackHeight = target.Height;

        double fallbackX = target.X + target.TranslationX;
        double fallbackY = target.Y + target.TranslationY;

        Element? current = target.Parent;

        while (current is VisualElement parentVisual && current != relativeTo)
        {
            if (parentVisual is ScrollView scrollView)
            {
                fallbackX -= scrollView.ScrollX;
                fallbackY -= scrollView.ScrollY;
            }

            fallbackX += parentVisual.X + parentVisual.TranslationX;
            fallbackY += parentVisual.Y + parentVisual.TranslationY;

            current = current.Parent;
        }

        return new Rect(fallbackX, fallbackY, fallbackWidth, fallbackHeight);
    }
}

