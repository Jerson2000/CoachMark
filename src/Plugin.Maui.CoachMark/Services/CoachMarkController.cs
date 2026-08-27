namespace Plugin.Maui.CoachMark;

/// <summary>
/// Manages the visual lifecycle, state transitions, event subscriptions, and overlay hosting for a coach mark tour.
/// Guarantees that all UI visual tree mutations and animations execute strictly on the Main/UI thread.
/// </summary>
public sealed class CoachMarkController : IDisposable
{
    private readonly IDispatcher _dispatcher;
    private readonly ICoordinateResolver _coordinateResolver;
    private readonly ITooltipPlacementEngine _placementEngine;
    private readonly IElementTracker _elementTracker;
    private readonly SemaphoreSlim _transitionLock = new(1, 1);

    private CoachMarkOverlayView? _overlayView;
    private WeakReference<ContentPage>? _hostPageWeakRef;
    private View? _originalPageContent;
    private Grid? _wrapperGrid;
    private bool _didWrapPageContent;

    private IReadOnlyList<CoachMarkStep>? _currentSteps;
    private CoachMarkOptions? _currentOptions;
    private int _currentStepIndex = -1;
    private CoachMarkState _state = CoachMarkState.Idle;
    private TaskCompletionSource<CoachMarkResult>? _tourCompletionSource;
    private CancellationTokenRegistration _cancellationRegistration;
    private bool _isDisposed;

    public bool IsActive => _state is CoachMarkState.Showing or CoachMarkState.Transitioning;
    public int CurrentStepIndex => _currentStepIndex;

    public CoachMarkController(
        IDispatcher dispatcher,
        ICoordinateResolver? coordinateResolver = null,
        ITooltipPlacementEngine? placementEngine = null,
        IElementTracker? elementTracker = null)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _coordinateResolver = coordinateResolver ?? new VisualTreeCoordinateResolver();
        _placementEngine = placementEngine ?? new TooltipPlacementEngine();
        _elementTracker = elementTracker ?? new ElementTracker(dispatcher);
        _elementTracker.TargetLayoutChanged += OnTargetLayoutChanged;
    }

    public Task<CoachMarkResult> StartTourAsync(
        IReadOnlyList<CoachMarkStep> steps,
        CoachMarkOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(steps);

        if (steps.Count == 0)
        {
            return Task.FromResult(new CoachMarkResult(CoachMarkDismissReason.Completed, -1, 0));
        }

        return RunOnMainThreadAsync(async () =>
        {
            await _transitionLock.WaitAsync(CancellationToken.None).ConfigureAwait(true);
            try
            {
                if (IsActive)
                {
                    await InternalDismissAsync(CoachMarkDismissReason.Programmatic).ConfigureAwait(true);
                }

                _currentSteps = steps;
                _currentOptions = options ?? new CoachMarkOptions();
                _currentStepIndex = -1;
                _state = CoachMarkState.Transitioning;
                _tourCompletionSource = new TaskCompletionSource<CoachMarkResult>();

                if (cancellationToken.CanBeCanceled)
                {
                    _cancellationRegistration = cancellationToken.Register(() =>
                    {
                        _ = DismissAsync(CoachMarkDismissReason.Canceled);
                    });
                }

                bool attached = await AttachOverlayToCurrentPageAsync().ConfigureAwait(true);
                if (!attached)
                {
                    _state = CoachMarkState.Idle;
                    return new CoachMarkResult(CoachMarkDismissReason.TargetDetached, -1, steps.Count);
                }

                bool stepSuccess = await InternalGoToStepAsync(0, cancellationToken).ConfigureAwait(true);
                if (!stepSuccess)
                {
                    await InternalDismissAsync(CoachMarkDismissReason.TargetDetached).ConfigureAwait(true);
                }
            }
            catch (OperationCanceledException)
            {
                await InternalDismissAsync(CoachMarkDismissReason.Canceled).ConfigureAwait(true);
            }
            catch (Exception)
            {
                await InternalDismissAsync(CoachMarkDismissReason.Programmatic).ConfigureAwait(true);
                throw;
            }
            finally
            {
                _transitionLock.Release();
            }

            var tcs = _tourCompletionSource;
            return tcs != null
                ? await tcs.Task.ConfigureAwait(true)
                : new CoachMarkResult(CoachMarkDismissReason.Programmatic, -1, steps.Count);
        });
    }

    public Task NextAsync() => RunOnMainThreadAsync(async () =>
    {
        await _transitionLock.WaitAsync(CancellationToken.None).ConfigureAwait(true);
        try
        {
            if (!IsActive || _currentSteps == null)
            {
                return;
            }

            int nextIndex = _currentStepIndex + 1;
            if (nextIndex >= _currentSteps.Count)
            {
                await InternalDismissAsync(CoachMarkDismissReason.Completed).ConfigureAwait(true);
            }
            else
            {
                await InternalGoToStepAsync(nextIndex, CancellationToken.None).ConfigureAwait(true);
            }
        }
        finally
        {
            _transitionLock.Release();
        }
    });

    public Task PreviousAsync() => RunOnMainThreadAsync(async () =>
    {
        await _transitionLock.WaitAsync(CancellationToken.None).ConfigureAwait(true);
        try
        {
            if (!IsActive || _currentSteps == null || _currentStepIndex <= 0)
            {
                return;
            }

            await InternalGoToStepAsync(_currentStepIndex - 1, CancellationToken.None).ConfigureAwait(true);
        }
        finally
        {
            _transitionLock.Release();
        }
    });

    public Task SkipAsync() => DismissAsync(CoachMarkDismissReason.Skipped);

    public Task DismissAsync(CoachMarkDismissReason reason = CoachMarkDismissReason.Programmatic) =>
        RunOnMainThreadAsync(async () =>
        {
            await _transitionLock.WaitAsync(CancellationToken.None).ConfigureAwait(true);
            try
            {
                await InternalDismissAsync(reason).ConfigureAwait(true);
            }
            finally
            {
                _transitionLock.Release();
            }
        });

    private async Task<bool> InternalGoToStepAsync(int targetIndex, CancellationToken cancellationToken)
    {
        if (_currentSteps == null || targetIndex < 0 || targetIndex >= _currentSteps.Count || _overlayView == null)
        {
            return false;
        }

        _state = CoachMarkState.Transitioning;
        var step = _currentSteps[targetIndex];
        var options = _currentOptions ?? new CoachMarkOptions();

        if (step.BeforeShowAsync != null)
        {
            bool proceed = await step.BeforeShowAsync().ConfigureAwait(true);
            if (!proceed || cancellationToken.IsCancellationRequested)
            {
                return false;
            }
        }

        var targetElement = await step.ResolveTargetAsync().ConfigureAwait(true);
        if (targetElement == null)
        {
            return false;
        }

        if (options.ScrollToTargetIfNeeded)
        {
            await ScrollToElementIfNeededAsync(targetElement).ConfigureAwait(true);
        }

        var targetBoundsNullable = _coordinateResolver.GetBoundingBox(targetElement, _overlayView);
        if (!targetBoundsNullable.HasValue)
        {
            return false;
        }

        var targetBounds = targetBoundsNullable.Value;
        _elementTracker.Track(targetElement);

        _currentStepIndex = targetIndex;
        options.OnStepAppearing?.Invoke(step, targetIndex);

        await _overlayView.TransitionToStepAsync(
            step,
            targetIndex,
            _currentSteps.Count,
            targetBounds,
            options,
            _placementEngine,
            cancellationToken).ConfigureAwait(true);

        _state = CoachMarkState.Showing;
        return true;
    }

    private async Task InternalDismissAsync(CoachMarkDismissReason reason)
    {
        if (_state == CoachMarkState.Idle && _tourCompletionSource?.Task.IsCompleted == true)
        {
            return;
        }

        _state = CoachMarkState.Dismissing;
        _elementTracker.Stop();
        _cancellationRegistration.Dispose();

        int lastIndex = _currentStepIndex;
        int total = _currentSteps?.Count ?? 0;

        if (_currentSteps != null && lastIndex >= 0 && lastIndex < _currentSteps.Count)
        {
            var step = _currentSteps[lastIndex];
            _currentOptions?.OnStepDisappeared?.Invoke(step, lastIndex);
            if (step.AfterDismissAsync != null)
            {
                try
                {
                    await step.AfterDismissAsync().ConfigureAwait(true);
                }
                catch
                {
                    // Cleanly suppress hook errors on shutdown
                }
            }
        }

        await DetachOverlayFromPageAsync().ConfigureAwait(true);

        _currentSteps = null;
        _currentStepIndex = -1;
        _state = CoachMarkState.Idle;

        _tourCompletionSource?.TrySetResult(new CoachMarkResult(reason, lastIndex, total));
    }

    private async Task<bool> AttachOverlayToCurrentPageAsync()
    {
        var page = GetActiveContentPage();
        if (page == null)
        {
            return false;
        }

        _hostPageWeakRef = new WeakReference<ContentPage>(page);
        page.Disappearing += OnPageDisappearing;
        page.Unloaded += OnPageUnloaded;

        _overlayView = new CoachMarkOverlayView();
        _overlayView.ApplyOptions(_currentOptions ?? new CoachMarkOptions());

        _overlayView.NextRequested += OnOverlayNextRequested;
        _overlayView.PreviousRequested += OnOverlayPreviousRequested;
        _overlayView.SkipRequested += OnOverlaySkipRequested;
        _overlayView.OutsideTapped += OnOverlayOutsideTapped;
        _overlayView.SpotlightTapped += OnOverlaySpotlightTapped;

        if (page.Content is Grid pageGrid)
        {
            _didWrapPageContent = false;
            if (pageGrid.RowDefinitions.Count > 0)
            {
                Grid.SetRowSpan(_overlayView, pageGrid.RowDefinitions.Count);
            }
            if (pageGrid.ColumnDefinitions.Count > 0)
            {
                Grid.SetColumnSpan(_overlayView, pageGrid.ColumnDefinitions.Count);
            }
            pageGrid.Children.Add(_overlayView);
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

            _wrapperGrid.Children.Add(_overlayView);
            page.Content = _wrapperGrid;
        }

        return true;
    }

    private Task DetachOverlayFromPageAsync()
    {
        if (_overlayView != null)
        {
            _overlayView.NextRequested -= OnOverlayNextRequested;
            _overlayView.PreviousRequested -= OnOverlayPreviousRequested;
            _overlayView.SkipRequested -= OnOverlaySkipRequested;
            _overlayView.OutsideTapped -= OnOverlayOutsideTapped;
            _overlayView.SpotlightTapped -= OnOverlaySpotlightTapped;
        }

        if (_hostPageWeakRef != null && _hostPageWeakRef.TryGetTarget(out var page))
        {
            page.Disappearing -= OnPageDisappearing;
            page.Unloaded -= OnPageUnloaded;

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

        _overlayView = null;
        _hostPageWeakRef = null;
        return Task.CompletedTask;
    }

    private void OnOverlayNextRequested(object? sender, EventArgs e) => _ = NextAsync();

    private void OnOverlayPreviousRequested(object? sender, EventArgs e) => _ = PreviousAsync();

    private void OnOverlaySkipRequested(object? sender, EventArgs e) => _ = SkipAsync();

    private void OnOverlayOutsideTapped(object? sender, EventArgs e)
    {
        var opt = _currentOptions;
        var step = _currentSteps != null && _currentStepIndex >= 0 && _currentStepIndex < _currentSteps.Count
            ? _currentSteps[_currentStepIndex]
            : null;

        bool dismiss = step?.DismissOnOutsideTap ?? opt?.DismissOnOutsideTap ?? false;
        if (dismiss)
        {
            _ = DismissAsync(CoachMarkDismissReason.OutsideTap);
        }
    }

    private void OnOverlaySpotlightTapped(object? sender, EventArgs e)
    {
        var opt = _currentOptions;
        var step = _currentSteps != null && _currentStepIndex >= 0 && _currentStepIndex < _currentSteps.Count
            ? _currentSteps[_currentStepIndex]
            : null;

        bool advance = step?.AdvanceOnTargetTap ?? opt?.AdvanceOnTargetTap ?? false;
        if (advance)
        {
            _ = NextAsync();
        }
    }

    private void OnPageDisappearing(object? sender, EventArgs e)
    {
        _ = DismissAsync(CoachMarkDismissReason.TargetDetached);
    }

    private void OnPageUnloaded(object? sender, EventArgs e)
    {
        _ = DismissAsync(CoachMarkDismissReason.TargetDetached);
    }

    private void OnTargetLayoutChanged(object? sender, EventArgs e)
    {
        _ = RunOnMainThreadAsync(async () =>
        {
            if (!IsActive || _overlayView == null || _currentSteps == null || _currentStepIndex < 0 || _currentStepIndex >= _currentSteps.Count)
            {
                return;
            }

            var step = _currentSteps[_currentStepIndex];
            var target = await step.ResolveTargetAsync().ConfigureAwait(true);
            if (target == null || !target.IsVisible)
            {
                return;
            }

            var bounds = _coordinateResolver.GetBoundingBox(target, _overlayView);
            if (bounds.HasValue && _currentOptions != null)
            {
                _overlayView.UpdateTargetPosition(bounds.Value, _currentOptions, _placementEngine);
            }
        });
    }

    private static async Task ScrollToElementIfNeededAsync(VisualElement target)
    {
        Element? current = target.Parent;
        ItemsView? foundItemsView = null;
#pragma warning disable CS0618
        ListView? foundListView = null;
#pragma warning restore CS0618
        ScrollView? foundScrollView = null;

        while (current != null)
        {
            if (current is ItemsView itemsView && foundItemsView == null)
            {
                foundItemsView = itemsView;
            }
#pragma warning disable CS0618
            else if (current is ListView listView && foundListView == null)
            {
                foundListView = listView;
            }
#pragma warning restore CS0618
            else if (current is ScrollView scrollView && foundScrollView == null)
            {
                foundScrollView = scrollView;
            }
            current = current.Parent;
        }

        bool didScroll = false;

        if (foundItemsView != null)
        {
            object itemToScroll = target.BindingContext ?? target;
            foundItemsView.ScrollTo(itemToScroll, position: ScrollToPosition.Center, animate: true);
            didScroll = true;
        }
#pragma warning disable CS0618
        else if (foundListView != null)
        {
            object itemToScroll = target.BindingContext ?? target;
            foundListView.ScrollTo(itemToScroll, ScrollToPosition.Center, true);
            didScroll = true;
        }
#pragma warning restore CS0618

        if (foundScrollView != null)
        {
            VisualElement targetOrContainer = (VisualElement?)foundItemsView ?? (VisualElement?)foundListView ?? target;
            await foundScrollView.ScrollToAsync(targetOrContainer, ScrollToPosition.MakeVisible, true).ConfigureAwait(true);
            didScroll = true;
        }

        if (didScroll)
        {
            // Give platform native scroll animations and layouts time to settle
            await Task.Delay(100).ConfigureAwait(true);
        }
    }

    internal static ContentPage? GetActiveContentPage(Page? rootPage = null)
    {
        var page = rootPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null)
        {
            return null;
        }

        var visitedPages = new HashSet<Page>();

        while (page != null && visitedPages.Add(page))
        {
            if (page.Navigation?.ModalStack?.Count > 0)
            {
                var topModal = page.Navigation.ModalStack.LastOrDefault();
                if (topModal != null && topModal != page && !visitedPages.Contains(topModal))
                {
                    page = topModal;
                    continue;
                }
            }

            if (page is Shell shell)
            {
                page = shell.CurrentPage;
                continue;
            }

            if (page is NavigationPage navPage)
            {
                page = navPage.CurrentPage;
                continue;
            }

            if (page is TabbedPage tabbedPage)
            {
                page = tabbedPage.CurrentPage;
                continue;
            }

            if (page is FlyoutPage flyoutPage)
            {
                page = flyoutPage.Detail;
                continue;
            }

            break;
        }

        return page as ContentPage;
    }

    private Task RunOnMainThreadAsync(Func<Task> asyncAction)
    {
        if (!IsDispatchRequired())
        {
            return asyncAction();
        }

        var tcs = new TaskCompletionSource<bool>();
        _dispatcher.Dispatch(async () =>
        {
            try
            {
                await asyncAction().ConfigureAwait(true);
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    private Task<T> RunOnMainThreadAsync<T>(Func<Task<T>> asyncFunc)
    {
        if (!IsDispatchRequired())
        {
            return asyncFunc();
        }

        var tcs = new TaskCompletionSource<T>();
        _dispatcher.Dispatch(async () =>
        {
            try
            {
                var result = await asyncFunc().ConfigureAwait(true);
                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });

        return tcs.Task;
    }

    private bool IsDispatchRequired()
    {
        try
        {
            return _dispatcher.IsDispatchRequired;
        }
        catch
        {
            return !MainThread.IsMainThread;
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _elementTracker.TargetLayoutChanged -= OnTargetLayoutChanged;
        _elementTracker.Dispose();
        _transitionLock.Dispose();
        _cancellationRegistration.Dispose();
    }
}
