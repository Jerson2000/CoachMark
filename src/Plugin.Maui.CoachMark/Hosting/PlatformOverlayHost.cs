#if ANDROID || IOS || MACCATALYST || WINDOWS
using Microsoft.Maui.Platform;
#endif

namespace Plugin.Maui.CoachMark;

/// <summary>
/// Hosts <see cref="CoachMarkOverlayView"/> either inside the active <see cref="ContentPage"/> content tree
/// or at the platform <see cref="Window"/> root so overlays cover native Shell navigation and tab bars.
/// </summary>
public sealed class PlatformOverlayHost : IOverlayHost
{
    private CoachMarkOverlayView? _overlayView;
    private WeakReference<ContentPage>? _hostPageWeakRef;
    private View? _originalPageContent;
    private Grid? _wrapperGrid;
    private bool _didWrapPageContent;

#if ANDROID
    private Android.Views.View? _androidPlatformView;
    private Android.Views.ViewGroup? _androidContentRoot;
#elif IOS || MACCATALYST
    private OverlayHostUIView? _appleHostView;
#elif WINDOWS
    private Microsoft.UI.Xaml.Controls.Primitives.Popup? _winPopup;
    private Microsoft.UI.Xaml.Controls.Grid? _winHostGrid;
    private Microsoft.UI.Xaml.FrameworkElement? _winPlatformElement;
    private Microsoft.UI.Xaml.XamlRoot? _winXamlRoot;
#endif

    public bool IsWindowHosted { get; private set; }

    public async Task<bool> AttachAsync(
        CoachMarkOverlayView overlayView,
        ContentPage page,
        Window? window,
        CoachMarkOverlayHostMode mode)
    {
        ArgumentNullException.ThrowIfNull(overlayView);
        ArgumentNullException.ThrowIfNull(page);

        await DetachAsync().ConfigureAwait(true);

        _overlayView = overlayView;

        if (mode == CoachMarkOverlayHostMode.Window)
        {
            if (TryAttachToWindow(overlayView, page, window))
            {
                IsWindowHosted = true;
                return true;
            }
        }

        IsWindowHosted = false;
        return AttachToPage(overlayView, page);
    }

    public Task DetachAsync()
    {
        if (IsWindowHosted)
        {
            DetachFromWindow();
            IsWindowHosted = false;
        }
        else
        {
            DetachFromPage();
        }

        if (_overlayView != null)
        {
            _overlayView.HostViewportSize = null;
        }

        _overlayView = null;
        return Task.CompletedTask;
    }

    private bool AttachToPage(CoachMarkOverlayView overlayView, ContentPage page)
    {
        _hostPageWeakRef = new WeakReference<ContentPage>(page);

        if (page.Content is Grid pageGrid)
        {
            _didWrapPageContent = false;
            if (pageGrid.RowDefinitions.Count > 0)
            {
                Grid.SetRowSpan(overlayView, pageGrid.RowDefinitions.Count);
            }

            if (pageGrid.ColumnDefinitions.Count > 0)
            {
                Grid.SetColumnSpan(overlayView, pageGrid.ColumnDefinitions.Count);
            }

            pageGrid.Children.Add(overlayView);
        }
        else
        {
            _didWrapPageContent = true;
            _originalPageContent = page.Content;

            _wrapperGrid = new Grid
            {
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };

            if (_originalPageContent != null)
            {
                page.Content = null;
                _wrapperGrid.Children.Add(_originalPageContent);
            }

            _wrapperGrid.Children.Add(overlayView);
            page.Content = _wrapperGrid;
        }

        return true;
    }

    private void DetachFromPage()
    {
        if (_hostPageWeakRef != null && _hostPageWeakRef.TryGetTarget(out var page))
        {
            if (_didWrapPageContent)
            {
                if (_wrapperGrid != null)
                {
                    _wrapperGrid.Children.Clear();
                    _wrapperGrid = null;
                }

                page.Content = _originalPageContent;
                _originalPageContent = null;
                _didWrapPageContent = false;
            }
            else if (page.Content is Grid pageGrid && _overlayView != null)
            {
                pageGrid.Children.Remove(_overlayView);
            }
        }

        _hostPageWeakRef = null;
        _originalPageContent = null;
        _wrapperGrid = null;
        _didWrapPageContent = false;
    }

    private bool TryAttachToWindow(CoachMarkOverlayView overlayView, ContentPage page, Window? window)
    {
#if ANDROID
        var mauiContext = window?.Handler?.MauiContext ?? page.Handler?.MauiContext;
        if (mauiContext == null)
        {
            return false;
        }

        var activity = window?.Handler?.PlatformView as Android.App.Activity
            ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

        var contentRoot = activity?.FindViewById<Android.Views.ViewGroup>(Android.Resource.Id.Content);
        if (contentRoot == null)
        {
            return false;
        }

        var platformView = overlayView.ToPlatform(mauiContext);
        platformView.Elevation = 1000f;

        float density = activity?.Resources?.DisplayMetrics?.Density
            ?? contentRoot.Context?.Resources?.DisplayMetrics?.Density
            ?? 1.0f;
        if (density <= 0f)
        {
            density = 1.0f;
        }

        int pxWidth = contentRoot.Width > 0 ? contentRoot.Width : (activity?.Resources?.DisplayMetrics?.WidthPixels ?? 0);
        int pxHeight = contentRoot.Height > 0 ? contentRoot.Height : (activity?.Resources?.DisplayMetrics?.HeightPixels ?? 0);
        if (pxWidth > 0 && pxHeight > 0)
        {
            overlayView.HostViewportSize = new Size(pxWidth / density, pxHeight / density);
        }

        var layoutParams = new Android.Widget.FrameLayout.LayoutParams(
            Android.Views.ViewGroup.LayoutParams.MatchParent,
            Android.Views.ViewGroup.LayoutParams.MatchParent);

        contentRoot.AddView(platformView, layoutParams);
        platformView.BringToFront();

        _androidPlatformView = platformView;
        _androidContentRoot = contentRoot;
        return true;
#elif IOS || MACCATALYST
        var mauiContext = window?.Handler?.MauiContext ?? page.Handler?.MauiContext;
        if (mauiContext == null)
        {
            return false;
        }

        var uiWindow = window?.Handler?.PlatformView as UIKit.UIWindow
            ?? Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as UIKit.UIWindow;
        if (uiWindow == null)
        {
            return false;
        }

        overlayView.HostViewportSize = new Size(uiWindow.Bounds.Width, uiWindow.Bounds.Height);
        var platformView = overlayView.ToPlatform(mauiContext);
        var hostView = new OverlayHostUIView(overlayView, platformView)
        {
            Frame = uiWindow.Bounds,
            AutoresizingMask = UIKit.UIViewAutoresizing.FlexibleWidth | UIKit.UIViewAutoresizing.FlexibleHeight,
            BackgroundColor = UIKit.UIColor.Clear
        };

        uiWindow.AddSubview(hostView);
        uiWindow.BringSubviewToFront(hostView);
        hostView.SetNeedsLayout();
        hostView.LayoutIfNeeded();

        _appleHostView = hostView;
        return true;
#elif WINDOWS
        var mauiContext = window?.Handler?.MauiContext ?? page.Handler?.MauiContext;
        if (mauiContext == null)
        {
            return false;
        }

        var winWindow = window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window
            ?? Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

        var xamlRoot = (winWindow?.Content as Microsoft.UI.Xaml.UIElement)?.XamlRoot
            ?? (page.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement)?.XamlRoot;
        if (xamlRoot == null)
        {
            return false;
        }

        var size = xamlRoot.Size;
        overlayView.HostViewportSize = new Size(size.Width, size.Height);

        var platformView = overlayView.ToPlatform(mauiContext);
        var platformElement = platformView as Microsoft.UI.Xaml.FrameworkElement;
        if (platformElement != null)
        {
            platformElement.Width = size.Width;
            platformElement.Height = size.Height;
            platformElement.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
            platformElement.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
        }

        var hostGrid = new Microsoft.UI.Xaml.Controls.Grid
        {
            Width = size.Width,
            Height = size.Height
        };
        hostGrid.Children.Add(platformView);

        var popup = new Microsoft.UI.Xaml.Controls.Primitives.Popup
        {
            XamlRoot = xamlRoot,
            IsLightDismissEnabled = false,
            HorizontalOffset = 0,
            VerticalOffset = 0,
            Child = hostGrid,
            IsOpen = true
        };

        xamlRoot.Changed += OnWinXamlRootChanged;
        hostGrid.UpdateLayout();
        if (size.Width > 0 && size.Height > 0)
        {
            ((IView)overlayView).Measure(size.Width, size.Height);
            ((IView)overlayView).Arrange(new Rect(0, 0, size.Width, size.Height));
        }

        _winXamlRoot = xamlRoot;
        _winHostGrid = hostGrid;
        _winPlatformElement = platformElement;
        _winPopup = popup;
        return true;
#else
        return false;
#endif
    }

    private void DetachFromWindow()
    {
#if ANDROID
        if (_androidContentRoot != null && _androidPlatformView != null)
        {
            _androidContentRoot.RemoveView(_androidPlatformView);
        }

        _androidPlatformView = null;
        _androidContentRoot = null;
        _overlayView?.Handler?.DisconnectHandler();
#elif IOS || MACCATALYST
        if (_appleHostView != null)
        {
            _appleHostView.RemoveFromSuperview();
            _appleHostView.Dispose();
            _appleHostView = null;
        }

        _overlayView?.Handler?.DisconnectHandler();
#elif WINDOWS
        if (_winXamlRoot != null)
        {
            _winXamlRoot.Changed -= OnWinXamlRootChanged;
            _winXamlRoot = null;
        }

        if (_winPopup != null)
        {
            _winPopup.IsOpen = false;
            _winPopup.Child = null;
            _winPopup = null;
        }

        if (_winHostGrid != null)
        {
            _winHostGrid.Children.Clear();
            _winHostGrid = null;
        }

        _winPlatformElement = null;
        _overlayView?.Handler?.DisconnectHandler();
#endif
    }

#if WINDOWS
    private void OnWinXamlRootChanged(Microsoft.UI.Xaml.XamlRoot sender, Microsoft.UI.Xaml.XamlRootChangedEventArgs args)
    {
        if (_winHostGrid == null)
        {
            return;
        }

        var size = sender.Size;
        _winHostGrid.Width = size.Width;
        _winHostGrid.Height = size.Height;

        if (_winPlatformElement != null)
        {
            _winPlatformElement.Width = size.Width;
            _winPlatformElement.Height = size.Height;
        }

        if (_overlayView != null && size.Width > 0 && size.Height > 0)
        {
            _overlayView.HostViewportSize = new Size(size.Width, size.Height);
            ((IView)_overlayView).Measure(size.Width, size.Height);
            ((IView)_overlayView).Arrange(new Rect(0, 0, size.Width, size.Height));
        }
    }
#endif

#if IOS || MACCATALYST
    private sealed class OverlayHostUIView : UIKit.UIView
    {
        private readonly CoachMarkOverlayView _overlayView;
        private readonly UIKit.UIView _childPlatformView;

        public OverlayHostUIView(CoachMarkOverlayView overlayView, UIKit.UIView childPlatformView)
        {
            _overlayView = overlayView;
            _childPlatformView = childPlatformView;
            AddSubview(childPlatformView);
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            _childPlatformView.Frame = Bounds;
            if (Bounds.Width > 0 && Bounds.Height > 0)
            {
                _overlayView.HostViewportSize = new Size(Bounds.Width, Bounds.Height);
                ((IView)_overlayView).Measure(Bounds.Width, Bounds.Height);
                ((IView)_overlayView).Arrange(new Rect(0, 0, Bounds.Width, Bounds.Height));
            }
        }
    }
#endif
}
