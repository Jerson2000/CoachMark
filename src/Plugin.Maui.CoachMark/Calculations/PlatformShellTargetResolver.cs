namespace Plugin.Maui.CoachMark;

/// <summary>
/// Platform-native resolver that locates Shell flyout (hamburger) and tab bar views
/// across Android, iOS/MacCatalyst, and Windows and converts their bounds to DIPs.
/// </summary>
public sealed class PlatformShellTargetResolver : IShellTargetResolver
{
    public Rect? GetBoundingBox(ShellTarget target, Window? window, VisualElement? relativeTo = null)
    {
        ArgumentNullException.ThrowIfNull(target);

#if ANDROID
        return GetAndroidBoundingBox(target, window, relativeTo);
#elif IOS || MACCATALYST
        return GetAppleBoundingBox(target, window, relativeTo);
#elif WINDOWS
        return GetWindowsBoundingBox(target, window, relativeTo);
#else
        return null;
#endif
    }

    internal static int? ResolveTabIndex(ShellTarget target)
    {
        if (target.Index.HasValue)
        {
            return target.Index.Value;
        }

        if (string.IsNullOrWhiteSpace(target.TitleOrRoute))
        {
            return null;
        }

        var shell = Shell.Current;
        var currentItem = shell?.CurrentItem;
        if (currentItem == null)
        {
            return null;
        }

        var sections = currentItem.Items;
        for (int i = 0; i < sections.Count; i++)
        {
            var section = sections[i];
            if (MatchesShellItem(section, target.TitleOrRoute) ||
                (section.Items.Count == 1 && MatchesShellItem(section.Items[0], target.TitleOrRoute)))
            {
                return i;
            }
        }

        // Handle top-tabs layout where a single ShellSection contains multiple ShellContent tabs
        if (sections.Count == 1 && sections[0].Items.Count > 1)
        {
            var contents = sections[0].Items;
            for (int i = 0; i < contents.Count; i++)
            {
                if (MatchesShellItem(contents[i], target.TitleOrRoute))
                {
                    return i;
                }
            }
        }

        return null;
    }

    private static bool MatchesShellItem(BaseShellItem? item, string titleOrRoute)
    {
        if (item == null)
        {
            return false;
        }

        return string.Equals(item.Title, titleOrRoute, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(item.Route, titleOrRoute, StringComparison.OrdinalIgnoreCase);
    }

    internal static int? ResolveFlyoutItemIndex(ShellTarget target)
    {
        if (target.Index.HasValue)
        {
            return target.Index.Value;
        }

        if (string.IsNullOrWhiteSpace(target.TitleOrRoute))
        {
            return null;
        }

        var shell = Shell.Current;
        if (shell == null)
        {
            return null;
        }

        int visibleIndex = 0;
        foreach (var item in shell.Items)
        {
            if (!item.FlyoutItemIsVisible)
            {
                continue;
            }

            if (MatchesShellItem(item, target.TitleOrRoute))
            {
                return visibleIndex;
            }

            visibleIndex++;
        }

        return null;
    }

#if ANDROID
    private static Rect? GetAndroidBoundingBox(ShellTarget target, Window? window, VisualElement? relativeTo)
    {
        var activity = window?.Handler?.PlatformView as Android.App.Activity
            ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

        var contentRoot = activity?.FindViewById<Android.Views.ViewGroup>(Android.Resource.Id.Content);
        if (contentRoot == null)
        {
            return null;
        }

        try
        {
            Android.Views.View? targetView = target.Kind switch
            {
                ShellTargetKind.FlyoutIcon => FindAndroidFlyoutButton(contentRoot),
                ShellTargetKind.TabBarItem => FindAndroidTabItemView(contentRoot, target),
                ShellTargetKind.FlyoutItem => FindAndroidFlyoutItemView(contentRoot, target),
                _ => null
            };

            if (targetView == null ||
                targetView.Visibility != Android.Views.ViewStates.Visible ||
                targetView.Width <= 0 ||
                targetView.Height <= 0)
            {
                return null;
            }

            int[] targetLocation = new int[2];
            targetView.GetLocationOnScreen(targetLocation);

            int targetX = targetLocation[0];
            int targetY = targetLocation[1];

            var relativeView = relativeTo?.Handler?.PlatformView as Android.Views.View ?? contentRoot;
            int[] relativeLocation = new int[2];
            relativeView.GetLocationOnScreen(relativeLocation);
            targetX -= relativeLocation[0];
            targetY -= relativeLocation[1];

            float density = targetView.Context?.Resources?.DisplayMetrics?.Density ?? 1.0f;
            if (density <= 0f)
            {
                density = 1.0f;
            }

            return new Rect(
                targetX / density,
                targetY / density,
                targetView.Width / density,
                targetView.Height / density);
        }
        catch
        {
            return null;
        }
    }

    private static Android.Views.View? FindAndroidFlyoutButton(Android.Views.ViewGroup root)
    {
        var toolbar = FindFirstVisibleDescendant<AndroidX.AppCompat.Widget.Toolbar>(root);
        if (toolbar == null)
        {
            return null;
        }

        for (int i = 0; i < toolbar.ChildCount; i++)
        {
            var child = toolbar.GetChildAt(i);
            if (child == null || child is AndroidX.AppCompat.Widget.ActionMenuView)
            {
                continue;
            }

            if (child is Android.Widget.ImageButton imageButton &&
                imageButton.Visibility == Android.Views.ViewStates.Visible &&
                imageButton.Width > 0 &&
                imageButton.Height > 0)
            {
                return imageButton;
            }
        }

        return null;
    }

    private static Android.Views.View? FindAndroidTabItemView(Android.Views.ViewGroup root, ShellTarget target)
    {
        var bottomNav = FindFirstVisibleDescendant<Google.Android.Material.BottomNavigation.BottomNavigationView>(root);
        if (bottomNav != null && bottomNav.ChildCount > 0 && bottomNav.GetChildAt(0) is Android.Views.ViewGroup menuView)
        {
            int? index = target.Index;
            if (!index.HasValue && !string.IsNullOrWhiteSpace(target.TitleOrRoute))
            {
                var menu = bottomNav.Menu;
                for (int i = 0; i < menu.Size(); i++)
                {
                    var menuItem = menu.GetItem(i);
                    var itemTitle = menuItem?.TitleFormatted?.ToString();
                    if (string.Equals(itemTitle, target.TitleOrRoute, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }

                index ??= ResolveTabIndex(target);
            }

            if (index.HasValue && index.Value >= 0 && index.Value < menuView.ChildCount)
            {
                return menuView.GetChildAt(index.Value);
            }
        }

        var tabLayout = FindFirstVisibleDescendant<Google.Android.Material.Tabs.TabLayout>(root);
        if (tabLayout != null && tabLayout.ChildCount > 0 && tabLayout.GetChildAt(0) is Android.Views.ViewGroup tabStrip)
        {
            int? index = target.Index;
            if (!index.HasValue && !string.IsNullOrWhiteSpace(target.TitleOrRoute))
            {
                for (int i = 0; i < tabLayout.TabCount; i++)
                {
                    var tab = tabLayout.GetTabAt(i);
                    if (string.Equals(tab?.Text, target.TitleOrRoute, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }

                index ??= ResolveTabIndex(target);
            }

            if (index.HasValue && index.Value >= 0 && index.Value < tabStrip.ChildCount)
            {
                return tabStrip.GetChildAt(index.Value);
            }
        }

        return null;
    }

    private static Android.Views.View? FindAndroidFlyoutItemView(Android.Views.ViewGroup root, ShellTarget target)
    {
        var drawerLayout = FindFirstVisibleDescendant<AndroidX.DrawerLayout.Widget.DrawerLayout>(root);
        if (drawerLayout == null)
        {
            return null;
        }

        var drawerChildren = new List<Android.Views.ViewGroup>();
        for (int i = 0; i < drawerLayout.ChildCount; i++)
        {
            var child = drawerLayout.GetChildAt(i);
            if (child is Android.Views.ViewGroup drawerGroup &&
                child.LayoutParameters is AndroidX.DrawerLayout.Widget.DrawerLayout.LayoutParams lp &&
                lp.Gravity != (int)Android.Views.GravityFlags.NoGravity)
            {
                drawerChildren.Add(drawerGroup);
            }
        }

        AndroidX.RecyclerView.Widget.RecyclerView? recyclerView = null;
        foreach (var drawerGroup in drawerChildren)
        {
            if (IsDrawerOpenSafe(drawerLayout, drawerGroup))
            {
                recyclerView = FindFirstVisibleDescendant<AndroidX.RecyclerView.Widget.RecyclerView>(drawerGroup);
                if (recyclerView != null)
                {
                    break;
                }
            }
        }

        if (recyclerView == null)
        {
            foreach (var drawerGroup in drawerChildren)
            {
                recyclerView = FindFirstVisibleDescendant<AndroidX.RecyclerView.Widget.RecyclerView>(drawerGroup);
                if (recyclerView != null)
                {
                    break;
                }
            }
        }

        if (recyclerView == null)
        {
            return null;
        }

        var visibleRows = new List<Android.Views.View>();
        for (int i = 0; i < recyclerView.ChildCount; i++)
        {
            var child = recyclerView.GetChildAt(i);
            if (child != null &&
                child.Visibility == Android.Views.ViewStates.Visible &&
                child.Width > 0 &&
                child.Height > 0)
            {
                visibleRows.Add(child);
            }
        }

        if (visibleRows.Count == 0)
        {
            return null;
        }

        visibleRows.Sort((a, b) => a.Top.CompareTo(b.Top));

        if (!string.IsNullOrWhiteSpace(target.TitleOrRoute))
        {
            foreach (var row in visibleRows)
            {
                if (HasMatchingAndroidText(row, target.TitleOrRoute))
                {
                    return row;
                }
            }
        }

        int? index = ResolveFlyoutItemIndex(target);
        if (!index.HasValue)
        {
            return null;
        }

        var textRows = visibleRows.Where(HasAnyNonEmptyAndroidText).ToList();
        if (index.Value >= 0 && index.Value < textRows.Count)
        {
            return textRows[index.Value];
        }

        return index.Value >= 0 && index.Value < visibleRows.Count ? visibleRows[index.Value] : null;
    }

    private static bool IsDrawerOpenSafe(
        AndroidX.DrawerLayout.Widget.DrawerLayout drawerLayout,
        Android.Views.View drawerChild)
    {
        try
        {
            return drawerLayout.IsDrawerOpen(drawerChild);
        }
        catch
        {
            return false;
        }
    }

    private static bool HasMatchingAndroidText(Android.Views.View view, string expected)
    {
        if (view is Android.Widget.TextView tv &&
            !string.IsNullOrWhiteSpace(tv.Text) &&
            string.Equals(tv.Text, expected, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (view is Android.Views.ViewGroup vg)
        {
            for (int i = 0; i < vg.ChildCount; i++)
            {
                var child = vg.GetChildAt(i);
                if (child != null && HasMatchingAndroidText(child, expected))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasAnyNonEmptyAndroidText(Android.Views.View view)
    {
        if (view is Android.Widget.TextView tv && !string.IsNullOrWhiteSpace(tv.Text))
        {
            return true;
        }

        if (view is Android.Views.ViewGroup vg)
        {
            for (int i = 0; i < vg.ChildCount; i++)
            {
                var child = vg.GetChildAt(i);
                if (child != null && HasAnyNonEmptyAndroidText(child))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static T? FindFirstVisibleDescendant<T>(Android.Views.ViewGroup parent) where T : Android.Views.View
    {
        var queue = new Queue<Android.Views.ViewGroup>();
        queue.Enqueue(parent);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            for (int i = 0; i < current.ChildCount; i++)
            {
                var child = current.GetChildAt(i);
                if (child == null || child.Visibility != Android.Views.ViewStates.Visible)
                {
                    continue;
                }

                if (child is T match && match.Width > 0 && match.Height > 0)
                {
                    return match;
                }

                if (child is Android.Views.ViewGroup childGroup)
                {
                    queue.Enqueue(childGroup);
                }
            }
        }

        return null;
    }
#endif

#if IOS || MACCATALYST
    private static Rect? GetAppleBoundingBox(ShellTarget target, Window? window, VisualElement? relativeTo)
    {
        var uiWindow = window?.Handler?.PlatformView as UIKit.UIWindow
            ?? Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as UIKit.UIWindow;

        if (uiWindow == null)
        {
            return null;
        }

        UIKit.UIView? targetView = target.Kind switch
        {
            ShellTargetKind.FlyoutIcon => FindAppleFlyoutButton(uiWindow),
            ShellTargetKind.TabBarItem => FindAppleTabItemView(uiWindow, target),
            ShellTargetKind.FlyoutItem => FindAppleFlyoutItemView(uiWindow, target),
            _ => null
        };

        if (targetView == null ||
            targetView.Hidden ||
            targetView.Bounds.Width <= 0 ||
            targetView.Bounds.Height <= 0)
        {
            return null;
        }

        try
        {
            var relativeView = relativeTo?.Handler?.PlatformView as UIKit.UIView ?? uiWindow;
            var converted = targetView.ConvertRectToView(targetView.Bounds, relativeView);
            return new Rect(converted.X, converted.Y, converted.Width, converted.Height);
        }
        catch
        {
            return null;
        }
    }

    private static UIKit.UIView? FindAppleFlyoutButton(UIKit.UIWindow uiWindow)
    {
        var navBar = FindFirstVisibleAppleDescendant<UIKit.UINavigationBar>(uiWindow);
        if (navBar == null)
        {
            return null;
        }

        var leftBarButton = navBar.TopItem?.LeftBarButtonItem;
        if (leftBarButton != null)
        {
            try
            {
                if (leftBarButton.ValueForKey(new Foundation.NSString("view")) is UIKit.UIView barItemView &&
                    !barItemView.Hidden &&
                    barItemView.Bounds.Width > 0 &&
                    barItemView.Bounds.Height > 0)
                {
                    return barItemView;
                }
            }
            catch
            {
                // Fall back to subview search if KVC lookup is unavailable
            }
        }

        var controls = new List<(UIKit.UIControl Control, double X)>();
        CollectVisibleAppleControls(navBar, navBar, controls);

        if (controls.Count == 0)
        {
            return null;
        }

        controls.Sort((a, b) => a.X.CompareTo(b.X));
        var leftmost = controls[0];
        return leftmost.X <= navBar.Bounds.Width * 0.5 ? leftmost.Control : null;
    }

    private static UIKit.UIView? FindAppleTabItemView(UIKit.UIWindow uiWindow, ShellTarget target)
    {
        var tabBar = FindFirstVisibleAppleDescendant<UIKit.UITabBar>(uiWindow);
        if (tabBar == null)
        {
            return null;
        }

        var controls = new List<(UIKit.UIControl Control, double X)>();
        CollectVisibleAppleControls(tabBar, tabBar, controls);

        if (controls.Count == 0)
        {
            return null;
        }

        controls.Sort((a, b) => a.X.CompareTo(b.X));

        int? index = target.Index;
        if (!index.HasValue && !string.IsNullOrWhiteSpace(target.TitleOrRoute))
        {
            var items = tabBar.Items;
            if (items != null)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (string.Equals(items[i].Title, target.TitleOrRoute, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }
            }

            index ??= ResolveTabIndex(target);
        }

        if (index.HasValue && index.Value >= 0 && index.Value < controls.Count)
        {
            return controls[index.Value].Control;
        }

        return null;
    }

    private static UIKit.UIView? FindAppleFlyoutItemView(UIKit.UIWindow uiWindow, ShellTarget target)
    {
        var tableViews = new List<(UIKit.UITableView Table, double X)>();
        CollectVisibleAppleViews(uiWindow, uiWindow, tableViews);
        if (tableViews.Count == 0)
        {
            return null;
        }

        tableViews.Sort((a, b) => a.X.CompareTo(b.X));

        foreach (var (table, _) in tableViews)
        {
            var cells = table.VisibleCells?
                .Where(c => !c.Hidden && c.Bounds.Width > 0 && c.Bounds.Height > 0)
                .OrderBy(c => c.Frame.Y)
                .ToList();

            if (cells == null || cells.Count == 0)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(target.TitleOrRoute))
            {
                foreach (var cell in cells)
                {
                    if (HasMatchingAppleLabel(cell, target.TitleOrRoute))
                    {
                        return cell;
                    }
                }
            }
            else
            {
                int? index = ResolveFlyoutItemIndex(target);
                if (index.HasValue && index.Value >= 0 && index.Value < cells.Count)
                {
                    return cells[index.Value];
                }
            }
        }

        int? fallbackIndex = ResolveFlyoutItemIndex(target);
        if (fallbackIndex.HasValue)
        {
            foreach (var (table, _) in tableViews)
            {
                var cells = table.VisibleCells?
                    .Where(c => !c.Hidden && c.Bounds.Width > 0 && c.Bounds.Height > 0)
                    .OrderBy(c => c.Frame.Y)
                    .ToList();

                if (cells != null && fallbackIndex.Value >= 0 && fallbackIndex.Value < cells.Count)
                {
                    return cells[fallbackIndex.Value];
                }
            }
        }

        return null;
    }

    private static void CollectVisibleAppleViews<T>(
        UIKit.UIView parent,
        UIKit.UIView coordinateSpace,
        List<(T View, double X)> results) where T : UIKit.UIView
    {
        foreach (var subview in parent.Subviews)
        {
            if (subview.Hidden || subview.Alpha <= 0.01f)
            {
                continue;
            }

            if (subview is T match && match.Bounds.Width > 0 && match.Bounds.Height > 0)
            {
                var converted = match.ConvertRectToView(match.Bounds, coordinateSpace);
                results.Add((match, converted.X));
                continue;
            }

            CollectVisibleAppleViews(subview, coordinateSpace, results);
        }
    }

    private static bool HasMatchingAppleLabel(UIKit.UIView view, string expected)
    {
        if (view is UIKit.UITableViewCell cell &&
            cell.ContentConfiguration is UIKit.UIListContentConfiguration listConfig &&
            !string.IsNullOrWhiteSpace(listConfig.Text) &&
            string.Equals(listConfig.Text, expected, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (view is UIKit.UILabel label &&
            !string.IsNullOrWhiteSpace(label.Text) &&
            string.Equals(label.Text, expected, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        foreach (var subview in view.Subviews)
        {
            if (HasMatchingAppleLabel(subview, expected))
            {
                return true;
            }
        }

        return false;
    }

    private static T? FindFirstVisibleAppleDescendant<T>(UIKit.UIView root) where T : UIKit.UIView
    {
        var queue = new Queue<UIKit.UIView>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var subview in current.Subviews)
            {
                if (subview.Hidden || subview.Alpha <= 0.01f)
                {
                    continue;
                }

                if (subview is T match && match.Bounds.Width > 0 && match.Bounds.Height > 0)
                {
                    return match;
                }

                queue.Enqueue(subview);
            }
        }

        return null;
    }

    private static void CollectVisibleAppleControls(
        UIKit.UIView parent,
        UIKit.UIView coordinateSpace,
        List<(UIKit.UIControl Control, double X)> results)
    {
        foreach (var subview in parent.Subviews)
        {
            if (subview.Hidden || subview.Alpha <= 0.01f)
            {
                continue;
            }

            if (subview is UIKit.UIControl control &&
                control.Bounds.Width > 0 &&
                control.Bounds.Height > 0)
            {
                var converted = control.ConvertRectToView(control.Bounds, coordinateSpace);
                results.Add((control, converted.X));
                continue;
            }

            CollectVisibleAppleControls(subview, coordinateSpace, results);
        }
    }
#endif

#if WINDOWS
    private static Rect? GetWindowsBoundingBox(ShellTarget target, Window? window, VisualElement? relativeTo)
    {
        var winWindow = window?.Handler?.PlatformView as Microsoft.UI.Xaml.Window
            ?? Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

        var rootElement = winWindow?.Content as Microsoft.UI.Xaml.FrameworkElement;
        if (rootElement == null)
        {
            return null;
        }

        var navView = FindFirstVisibleWinDescendant<Microsoft.UI.Xaml.Controls.NavigationView>(rootElement);
        if (navView == null)
        {
            return null;
        }

        Microsoft.UI.Xaml.FrameworkElement? targetElement = target.Kind switch
        {
            ShellTargetKind.FlyoutIcon => FindWinFlyoutToggleButton(navView),
            ShellTargetKind.TabBarItem => FindWinTabNavigationItem(rootElement, navView, target),
            ShellTargetKind.FlyoutItem => FindWinFlyoutNavigationItem(rootElement, navView, target),
            _ => null
        };

        if (targetElement == null ||
            targetElement.Visibility != Microsoft.UI.Xaml.Visibility.Visible ||
            targetElement.ActualWidth <= 0 ||
            targetElement.ActualHeight <= 0)
        {
            return null;
        }

        var relativeElement = relativeTo?.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement ?? rootElement;

        try
        {
            var transform = targetElement.TransformToVisual(relativeElement);
            var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
            return new Rect(point.X, point.Y, targetElement.ActualWidth, targetElement.ActualHeight);
        }
        catch
        {
            // When relativeTo is hosted in a WinUI Popup, TransformToVisual across visual roots fails;
            // fall back to rootElement which shares the Popup origin.
            if (!ReferenceEquals(relativeElement, rootElement))
            {
                try
                {
                    var transform = targetElement.TransformToVisual(rootElement);
                    var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                    return new Rect(point.X, point.Y, targetElement.ActualWidth, targetElement.ActualHeight);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }

    private static Microsoft.UI.Xaml.FrameworkElement? FindWinFlyoutToggleButton(
        Microsoft.UI.Xaml.Controls.NavigationView navView)
    {
        return FindFirstWinDescendant<Microsoft.UI.Xaml.Controls.Button>(
            navView,
            btn => string.Equals(btn.Name, "TogglePaneButton", StringComparison.Ordinal) &&
                   btn.Visibility == Microsoft.UI.Xaml.Visibility.Visible &&
                   btn.ActualWidth > 0 &&
                   btn.ActualHeight > 0);
    }

    private static Microsoft.UI.Xaml.FrameworkElement? FindWinTabNavigationItem(
        Microsoft.UI.Xaml.FrameworkElement rootElement,
        Microsoft.UI.Xaml.Controls.NavigationView fallbackNavView,
        ShellTarget target)
    {
        var tabNavView = FindFirstWinDescendant<Microsoft.UI.Xaml.Controls.NavigationView>(
            rootElement,
            nv => nv.PaneDisplayMode == Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.Top &&
                  nv.Visibility == Microsoft.UI.Xaml.Visibility.Visible &&
                  nv.ActualWidth > 0 &&
                  nv.ActualHeight > 0) ?? fallbackNavView;

        var items = new List<Microsoft.UI.Xaml.Controls.NavigationViewItem>();
        CollectWinItemsInNavigationView(tabNavView, tabNavView, items);

        return MatchWinNavigationViewItem(items, target, ResolveTabIndex);
    }

    private static Microsoft.UI.Xaml.FrameworkElement? FindWinFlyoutNavigationItem(
        Microsoft.UI.Xaml.FrameworkElement rootElement,
        Microsoft.UI.Xaml.Controls.NavigationView fallbackNavView,
        ShellTarget target)
    {
        var flyoutNavView = FindFirstWinDescendant<Microsoft.UI.Xaml.Controls.NavigationView>(
            rootElement,
            nv => nv.PaneDisplayMode != Microsoft.UI.Xaml.Controls.NavigationViewPaneDisplayMode.Top &&
                  nv.Visibility == Microsoft.UI.Xaml.Visibility.Visible &&
                  nv.ActualWidth > 0 &&
                  nv.ActualHeight > 0) ?? fallbackNavView;

        var items = new List<Microsoft.UI.Xaml.Controls.NavigationViewItem>();
        CollectWinItemsInNavigationView(flyoutNavView, flyoutNavView, items);

        return MatchWinNavigationViewItem(items, target, ResolveFlyoutItemIndex);
    }

    private static Microsoft.UI.Xaml.FrameworkElement? MatchWinNavigationViewItem(
        List<Microsoft.UI.Xaml.Controls.NavigationViewItem> items,
        ShellTarget target,
        Func<ShellTarget, int?> indexFallbackResolver)
    {
        if (items.Count == 0)
        {
            return null;
        }

        if (target.Index.HasValue)
        {
            int idx = target.Index.Value;
            return idx >= 0 && idx < items.Count ? items[idx] : null;
        }

        if (!string.IsNullOrWhiteSpace(target.TitleOrRoute))
        {
            foreach (var item in items)
            {
                if (item.DataContext is BaseShellItem shellItem && MatchesShellItem(shellItem, target.TitleOrRoute))
                {
                    return item;
                }

                var contentText = item.Content?.ToString();
                if (!string.IsNullOrWhiteSpace(contentText) &&
                    string.Equals(contentText, target.TitleOrRoute, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }

                var autoName = Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(item);
                if (!string.IsNullOrWhiteSpace(autoName) &&
                    string.Equals(autoName, target.TitleOrRoute, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }

                var matchingTextBlock = FindFirstWinDescendant<Microsoft.UI.Xaml.Controls.TextBlock>(
                    item,
                    tb => !string.IsNullOrWhiteSpace(tb.Text) &&
                          string.Equals(tb.Text, target.TitleOrRoute, StringComparison.OrdinalIgnoreCase));
                if (matchingTextBlock != null)
                {
                    return item;
                }
            }

            int? resolvedIndex = indexFallbackResolver(target);
            if (resolvedIndex.HasValue && resolvedIndex.Value >= 0 && resolvedIndex.Value < items.Count)
            {
                return items[resolvedIndex.Value];
            }
        }

        return null;
    }

    private static T? FindFirstVisibleWinDescendant<T>(Microsoft.UI.Xaml.DependencyObject root)
        where T : Microsoft.UI.Xaml.FrameworkElement
    {
        return FindFirstWinDescendant<T>(
            root,
            el => el.Visibility == Microsoft.UI.Xaml.Visibility.Visible &&
                  el.ActualWidth > 0 &&
                  el.ActualHeight > 0);
    }

    private static T? FindFirstWinDescendant<T>(
        Microsoft.UI.Xaml.DependencyObject root,
        Func<T, bool> predicate) where T : Microsoft.UI.Xaml.DependencyObject
    {
        var queue = new Queue<Microsoft.UI.Xaml.DependencyObject>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(current);
            for (int i = 0; i < count; i++)
            {
                var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(current, i);
                if (child is T match && predicate(match))
                {
                    return match;
                }

                queue.Enqueue(child);
            }
        }

        return null;
    }

    private static void CollectWinItemsInNavigationView(
        Microsoft.UI.Xaml.Controls.NavigationView ownerNavView,
        Microsoft.UI.Xaml.DependencyObject current,
        List<Microsoft.UI.Xaml.Controls.NavigationViewItem> results)
    {
        int count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(current);
        for (int i = 0; i < count; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(current, i);
            if (child is Microsoft.UI.Xaml.Controls.NavigationView nestedNav && !ReferenceEquals(nestedNav, ownerNavView))
            {
                continue;
            }

            if (child is Microsoft.UI.Xaml.Controls.NavigationViewItem item &&
                !ReferenceEquals(item, ownerNavView.SettingsItem) &&
                !string.Equals(item.Name, "SettingsItem", StringComparison.Ordinal) &&
                item.Visibility == Microsoft.UI.Xaml.Visibility.Visible &&
                item.ActualWidth > 0 &&
                item.ActualHeight > 0)
            {
                results.Add(item);
            }

            CollectWinItemsInNavigationView(ownerNavView, child, results);
        }
    }
#endif
}
