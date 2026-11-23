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
    private readonly IShellTargetResolver _shellTargetResolver;
    private readonly IOverlayHost _overlayHost;
    private readonly SemaphoreSlim _transitionLock = new(1, 1);

    private CoachMarkOverlayView? _overlayView;
    private WeakReference<ContentPage>? _hostPageWeakRef;
    private WeakReference<Window>? _hostWindowWeakRef;

    private IReadOnlyList<CoachMarkStep>? _currentSteps;
    private CoachMarkOptions? _currentOptions;
    private int _currentStepIndex = -1;
    private int _disappearedNotifiedStepIndex = -1;
    private CoachMarkState _state = CoachMarkState.Idle;
    private TaskCompletionSource<CoachMarkResult>? _tourCompletionSource;
    private CancellationTokenRegistration _cancellationRegistration;
    private bool _isDisposed;

    public bool IsActive => _state is CoachMarkState.Showing or CoachMarkState.Transitioning;
    public int CurrentStepIndex => _currentStepIndex;
    internal Func<ContentPage?>? ActivePageResolver { get; init; }

    public CoachMarkController(
        IDispatcher dispatcher,
        ICoordinateResolver? coordinateResolver = null,
        ITooltipPlacementEngine? placementEngine = null,
        IElementTracker? elementTracker = null,
        IShellTargetResolver? shellTargetResolver = null,
        IOverlayHost? overlayHost = null)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _coordinateResolver = coordinateResolver ?? new VisualTreeCoordinateResolver();
        _placementEngine = placementEngine ?? new TooltipPlacementEngine();
        _elementTracker = elementTracker ?? new ElementTracker(dispatcher);
        _shellTargetResolver = shellTargetResolver ?? new PlatformShellTargetResolver();
        _overlayHost = overlayHost ?? new PlatformOverlayHost();
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
                _disappearedNotifiedStepIndex = -1;
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

                bool stepSuccess = await TryAdvanceFromAsync(0, 1, cancellationToken).ConfigureAwait(true);
                if (!stepSuccess)
                {
                    var reason = cancellationToken.IsCancellationRequested
                        ? CoachMarkDismissReason.Canceled
                        : CoachMarkDismissReason.TargetDetached;
                    await InternalDismissAsync(reason).ConfigureAwait(true);
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

            await NotifyStepDisappearingAsync(_currentStepIndex).ConfigureAwait(true);

            bool moved = await TryAdvanceFromAsync(_currentStepIndex + 1, 1, CancellationToken.None).ConfigureAwait(true);
            if (!moved)
            {
                await InternalDismissAsync(CoachMarkDismissReason.Completed).ConfigureAwait(true);
            }
        }
        finally
        {
            if (_state == CoachMarkState.Transitioning)
            {
                _state = CoachMarkState.Showing;
            }
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

            await NotifyStepDisappearingAsync(_currentStepIndex).ConfigureAwait(true);

            bool moved = await TryAdvanceFromAsync(_currentStepIndex - 1, -1, CancellationToken.None).ConfigureAwait(true);
            if (!moved && _state == CoachMarkState.Transitioning)
            {
                _state = CoachMarkState.Showing;
            }
        }
        finally
        {
            if (_state == CoachMarkState.Transitioning)
            {
                _state = CoachMarkState.Showing;
            }
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

        if (_overlayHost.IsWindowHosted && (_overlayView.Width <= 0 || _overlayView.Height <= 0))
        {
            await WaitForOverlayLayoutAsync(_overlayView, TimeSpan.FromMilliseconds(100)).ConfigureAwait(true);
        }

        var targetBoundsNullable = await ResolveStepBoundsAsync(
            step,
            options,
            GetActiveWindow(),
            _overlayView,
            allowScroll: true,
            updateTracking: true).ConfigureAwait(true);

        if (!targetBoundsNullable.HasValue)
        {
            return false;
        }

        var targetBounds = targetBoundsNullable.Value;

        _currentStepIndex = targetIndex;
        _disappearedNotifiedStepIndex = -1;
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

    internal async Task<bool> TryAdvanceFromAsync(int startIndex, int direction, CancellationToken cancellationToken)
    {
        if (_currentSteps == null)
        {
            return false;
        }

        for (int index = startIndex; index >= 0 && index < _currentSteps.Count; index += direction)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (await InternalGoToStepAsync(index, cancellationToken).ConfigureAwait(true))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task WaitForOverlayLayoutAsync(CoachMarkOverlayView overlayView, TimeSpan timeout)
    {
        if (overlayView.Width > 0 && overlayView.Height > 0)
        {
            return;
        }

        var layoutReadyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnSizeChanged(object? sender, EventArgs e)
        {
            if (overlayView.Width > 0 && overlayView.Height > 0)
            {
                layoutReadyTcs.TrySetResult(true);
            }
        }

        overlayView.SizeChanged += OnSizeChanged;
        try
        {
            if (overlayView.Width > 0 && overlayView.Height > 0)
            {
                return;
            }

            await Task.WhenAny(layoutReadyTcs.Task, Task.Delay(timeout)).ConfigureAwait(true);
        }
        finally
        {
            overlayView.SizeChanged -= OnSizeChanged;
        }
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

        await NotifyStepDisappearingAsync(lastIndex).ConfigureAwait(true);

        await DetachOverlayFromPageAsync().ConfigureAwait(true);

        _currentSteps = null;
        _currentStepIndex = -1;
        _disappearedNotifiedStepIndex = -1;
        _state = CoachMarkState.Idle;

        _tourCompletionSource?.TrySetResult(new CoachMarkResult(reason, lastIndex, total));
    }

    private async Task NotifyStepDisappearingAsync(int stepIndex)
    {
        if (_currentSteps == null || stepIndex < 0 || stepIndex >= _currentSteps.Count || _disappearedNotifiedStepIndex == stepIndex)
        {
            return;
        }

        _disappearedNotifiedStepIndex = stepIndex;
        var step = _currentSteps[stepIndex];
        _currentOptions?.OnStepDisappeared?.Invoke(step, stepIndex);
        if (step.AfterDismissAsync != null)
        {
            try
            {
                await step.AfterDismissAsync().ConfigureAwait(true);
            }
            catch
            {
                // Cleanly suppress hook errors on step transition or shutdown
            }
        }
    }

    internal static CoachMarkOverlayHostMode ResolveEffectiveHostMode(
        IReadOnlyList<CoachMarkStep> steps,
        CoachMarkOptions options)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(options);

        if (options.OverlayHostMode != CoachMarkOverlayHostMode.Auto)
        {
            return options.OverlayHostMode;
        }

        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i].TargetKind == CoachMarkTargetKind.ShellElement)
            {
                return CoachMarkOverlayHostMode.Window;
            }
        }

        return CoachMarkOverlayHostMode.Page;
    }

    internal async Task<Rect?> ResolveStepBoundsAsync(
        CoachMarkStep step,
        CoachMarkOptions options,
        Window? activeWindow,
        VisualElement? relativeTo,
        bool allowScroll,
        bool updateTracking = true)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(options);

        switch (step.TargetKind)
        {
            case CoachMarkTargetKind.VisualElement:
            {
                var targetElement = await step.ResolveTargetAsync().ConfigureAwait(true);
                if (targetElement == null)
                {
                    return null;
                }

                if (allowScroll && options.ScrollToTargetIfNeeded)
                {
                    await ScrollToElementIfNeededAsync(targetElement).ConfigureAwait(true);
                }

                var bounds = _coordinateResolver.GetBoundingBox(targetElement, relativeTo);
                if (!bounds.HasValue)
                {
                    return null;
                }

                if (updateTracking)
                {
                    _elementTracker.Track(targetElement);
                }

                return bounds.Value;
            }

            case CoachMarkTargetKind.RelativeRect:
            {
                var anchorElement = await step.ResolveTargetAsync().ConfigureAwait(true);
                if (anchorElement == null)
                {
                    return null;
                }

                if (allowScroll && options.ScrollToTargetIfNeeded)
                {
                    await ScrollToElementIfNeededAsync(anchorElement).ConfigureAwait(true);
                }

                var anchorBounds = _coordinateResolver.GetBoundingBox(anchorElement, relativeTo);
                var localRect = await step.ResolveTargetRectAsync().ConfigureAwait(true);
                if (!anchorBounds.HasValue || !localRect.HasValue)
                {
                    return null;
                }

                if (updateTracking)
                {
                    _elementTracker.Track(anchorElement);
                }

                return new Rect(
                    anchorBounds.Value.X + localRect.Value.X,
                    anchorBounds.Value.Y + localRect.Value.Y,
                    localRect.Value.Width,
                    localRect.Value.Height);
            }

            case CoachMarkTargetKind.AbsoluteRect:
            {
                if (updateTracking)
                {
                    _elementTracker.Stop();
                }

                return await step.ResolveTargetRectAsync().ConfigureAwait(true);
            }

            case CoachMarkTargetKind.ShellElement:
            {
                if (updateTracking)
                {
                    _elementTracker.Stop();
                }

                if (step.ShellTarget == null)
                {
                    return null;
                }

                return _shellTargetResolver.GetBoundingBox(step.ShellTarget, activeWindow, relativeTo);
            }

            default:
                return null;
        }
    }

    private async Task<bool> AttachOverlayToCurrentPageAsync()
    {
        var page = ActivePageResolver?.Invoke() ?? GetActiveContentPage();
        if (page == null)
        {
            return false;
        }

        var options = _currentOptions ?? new CoachMarkOptions();
        var window = page.Window ?? Application.Current?.Windows.FirstOrDefault();

        _hostPageWeakRef = new WeakReference<ContentPage>(page);
        _hostWindowWeakRef = window != null ? new WeakReference<Window>(window) : null;
        page.Disappearing += OnPageDisappearing;
        page.Unloaded += OnPageUnloaded;

        _overlayView = new CoachMarkOverlayView();
        _overlayView.ApplyOptions(options);

        _overlayView.NextRequested += OnOverlayNextRequested;
        _overlayView.PreviousRequested += OnOverlayPreviousRequested;
        _overlayView.SkipRequested += OnOverlaySkipRequested;
        _overlayView.OutsideTapped += OnOverlayOutsideTapped;
        _overlayView.SpotlightTapped += OnOverlaySpotlightTapped;
        _overlayView.SizeChanged += OnOverlaySizeChanged;

        var mode = ResolveEffectiveHostMode(_currentSteps ?? Array.Empty<CoachMarkStep>(), options);
        bool attached = await _overlayHost.AttachAsync(_overlayView, page, window, mode).ConfigureAwait(true);
        if (!attached)
        {
            UnsubscribeOverlayEvents();
            page.Disappearing -= OnPageDisappearing;
            page.Unloaded -= OnPageUnloaded;
            _overlayView = null;
            _hostPageWeakRef = null;
            _hostWindowWeakRef = null;
            return false;
        }

        return true;
    }

    private async Task DetachOverlayFromPageAsync()
    {
        UnsubscribeOverlayEvents();

        if (_hostPageWeakRef != null && _hostPageWeakRef.TryGetTarget(out var page))
        {
            page.Disappearing -= OnPageDisappearing;
            page.Unloaded -= OnPageUnloaded;
        }

        await _overlayHost.DetachAsync().ConfigureAwait(true);

        _overlayView = null;
        _hostPageWeakRef = null;
        _hostWindowWeakRef = null;
    }

    private void UnsubscribeOverlayEvents()
    {
        if (_overlayView != null)
        {
            _overlayView.NextRequested -= OnOverlayNextRequested;
            _overlayView.PreviousRequested -= OnOverlayPreviousRequested;
            _overlayView.SkipRequested -= OnOverlaySkipRequested;
            _overlayView.OutsideTapped -= OnOverlayOutsideTapped;
            _overlayView.SpotlightTapped -= OnOverlaySpotlightTapped;
            _overlayView.SizeChanged -= OnOverlaySizeChanged;
        }
    }

    private Window? GetActiveWindow()
    {
        if (_hostWindowWeakRef != null && _hostWindowWeakRef.TryGetTarget(out var window))
        {
            return window;
        }

        return Application.Current?.Windows.FirstOrDefault();
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
        _ = RunOnMainThreadAsync(RefreshCurrentStepPositionAsync);
    }

    private void OnOverlaySizeChanged(object? sender, EventArgs e)
    {
        _ = RunOnMainThreadAsync(RefreshCurrentStepPositionAsync);
    }

    private async Task RefreshCurrentStepPositionAsync()
    {
        if (!IsActive || _overlayView == null || _currentSteps == null || _currentOptions == null ||
            _currentStepIndex < 0 || _currentStepIndex >= _currentSteps.Count)
        {
            return;
        }

        var step = _currentSteps[_currentStepIndex];
        var bounds = await ResolveStepBoundsAsync(
            step,
            _currentOptions,
            GetActiveWindow(),
            _overlayView,
            allowScroll: false,
            updateTracking: false).ConfigureAwait(true);

        if (bounds.HasValue)
        {
            _overlayView.UpdateTargetPosition(bounds.Value, _currentOptions, _placementEngine);
        }
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
