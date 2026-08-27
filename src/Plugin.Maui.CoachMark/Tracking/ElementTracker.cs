using System.ComponentModel;

namespace Plugin.Maui.CoachMark;

/// <summary>
/// Non-polling, event-driven layout and scroll tracker that coalesces updates per UI frame
/// and avoids retaining strong references to visual trees.
/// </summary>
public sealed class ElementTracker : IElementTracker
{
    private readonly IDispatcher _dispatcher;
    private readonly List<WeakReference<VisualElement>> _trackedAncestors = new();
    private WeakReference<VisualElement>? _trackedTarget;
    private bool _isDisposed;
    private bool _isUpdatePending;

    public event EventHandler? TargetLayoutChanged;

    public ElementTracker(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    public void Track(VisualElement target)
    {
        Stop();

        if (target == null)
        {
            return;
        }

        _trackedTarget = new WeakReference<VisualElement>(target);
        target.SizeChanged += OnTargetSizeChanged;
        target.PropertyChanged += OnTargetPropertyChanged;

        // Walk ancestors to subscribe to ScrollView/ItemsView.Scrolled and Container SizeChanged events
        Element? current = target.Parent;
        while (current is VisualElement parentVisual)
        {
            _trackedAncestors.Add(new WeakReference<VisualElement>(parentVisual));
            parentVisual.SizeChanged += OnAncestorSizeChanged;

            if (parentVisual is ScrollView scrollView)
            {
                scrollView.Scrolled += OnScrollViewScrolled;
            }
            else if (parentVisual is ItemsView itemsView)
            {
                itemsView.Scrolled += OnItemsViewScrolled;
            }
#pragma warning disable CS0618
            else if (parentVisual is ListView listView)
            {
                listView.Scrolled += OnListViewScrolled;
            }
#pragma warning restore CS0618

            current = current.Parent;
        }
    }

    public void Stop()
    {
        if (_trackedTarget != null && _trackedTarget.TryGetTarget(out var target))
        {
            target.SizeChanged -= OnTargetSizeChanged;
            target.PropertyChanged -= OnTargetPropertyChanged;
        }
        _trackedTarget = null;

        foreach (var weakAncestor in _trackedAncestors)
        {
            if (weakAncestor.TryGetTarget(out var ancestor))
            {
                ancestor.SizeChanged -= OnAncestorSizeChanged;
                if (ancestor is ScrollView scrollView)
                {
                    scrollView.Scrolled -= OnScrollViewScrolled;
                }
                else if (ancestor is ItemsView itemsView)
                {
                    itemsView.Scrolled -= OnItemsViewScrolled;
                }
#pragma warning disable CS0618
                else if (ancestor is ListView listView)
                {
                    listView.Scrolled -= OnListViewScrolled;
                }
#pragma warning restore CS0618
            }
        }
        _trackedAncestors.Clear();
        _isUpdatePending = false;
    }

    private void OnTargetSizeChanged(object? sender, EventArgs e) => TriggerThrottledUpdate();

    private void OnAncestorSizeChanged(object? sender, EventArgs e) => TriggerThrottledUpdate();

    private void OnScrollViewScrolled(object? sender, ScrolledEventArgs e) => TriggerThrottledUpdate();

    private void OnItemsViewScrolled(object? sender, ItemsViewScrolledEventArgs e) => TriggerThrottledUpdate();

    private void OnListViewScrolled(object? sender, ScrolledEventArgs e) => TriggerThrottledUpdate();

    private void OnTargetPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(VisualElement.TranslationX)
            or nameof(VisualElement.TranslationY)
            or nameof(VisualElement.IsVisible)
            or nameof(VisualElement.Scale)
            or nameof(VisualElement.Rotation)
            or nameof(VisualElement.X)
            or nameof(VisualElement.Y)
            or nameof(VisualElement.Width)
            or nameof(VisualElement.Height))
        {
            TriggerThrottledUpdate();
        }
    }

    private void TriggerThrottledUpdate()
    {
        if (_isDisposed || _isUpdatePending)
        {
            return;
        }

        _isUpdatePending = true;

        _dispatcher.Dispatch(() =>
        {
            if (_isDisposed)
            {
                return;
            }

            _isUpdatePending = false;
            TargetLayoutChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        Stop();
    }
}
