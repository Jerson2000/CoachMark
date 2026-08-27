using System.Collections.ObjectModel;
using Plugin.Maui.CoachMark;

namespace CoachMarkSample;

public sealed class SampleItem
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public partial class MainPage : ContentPage
{
    private readonly ICoachMarkService _coachMarkService;
    public ObservableCollection<SampleItem> Items { get; } = new();

    public MainPage(ICoachMarkService coachMarkService)
    {
        InitializeComponent();
        _coachMarkService = coachMarkService ?? throw new ArgumentNullException(nameof(coachMarkService));

        // Populate sample data
        Items.Add(new SampleItem { Name = "Cloud Database Sync", Description = "Configures automated backup and syncing." });
        Items.Add(new SampleItem { Name = "Security & Encryption", Description = "AES-256 local encrypted storage." });
        Items.Add(new SampleItem { Name = "Push Notifications", Description = "APNs and FCM background messaging." });
        Items.Add(new SampleItem { Name = "Analytics & Telemetry", Description = "Usage diagnostics and real-time performance." });
        Items.Add(new SampleItem { Name = "User Authentication", Description = "OAuth2, OpenID Connect, and Biometric auth." });
        Items.Add(new SampleItem { Name = "Offline Caching", Description = "Indexed fast local data cache and sync queue." });

        ItemsCollectionView.ItemsSource = Items;
    }

    private async void OnStartTourClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = "Tour started...";

        var steps = new List<CoachMarkStep>
        {
            new CoachMarkStep(
                target: SearchBox,
                title: "Instant Search",
                description: "Search across all database items and resources instantly.")
            {
                Placement = CoachMarkPlacement.Bottom,
                Shape = SpotlightShape.RoundedRectangle,
                SpotlightCornerRadius = 10f
            },
            new CoachMarkStep(
                target: SettingsBtn,
                title: "App Settings",
                description: "Access preferences, theme colors, and account configurations.")
            {
                Placement = CoachMarkPlacement.Bottom,
                Shape = SpotlightShape.Circle
            },
            new CoachMarkStep(
                target: DeepActionButton,
                title: "Deep Scroll Target",
                description: "The tour automatically scrolled down to bring this nested control into view!")
            {
                Placement = CoachMarkPlacement.Top,
                Shape = SpotlightShape.RoundedRectangle,
                SpotlightBorderColor = Color.FromArgb("#A78BFA"),
                SpotlightBorderWidth = 2f
            },
            new CoachMarkStep(
                target: CreateNewItemBtn,
                title: "Create Anything",
                description: "Tap here whenever you want to add a new project or record.")
            {
                Placement = CoachMarkPlacement.Top,
                Shape = SpotlightShape.RoundedRectangle,
                SpotlightCornerRadius = 24f
            }
        };

        var options = new CoachMarkOptions
        {
            OverlayOpacity = 0.75f,
            SpotlightBorderColor = Color.FromArgb("#818CF8"),
            SpotlightBorderWidth = 2f,
            AnimationDuration = TimeSpan.FromMilliseconds(300),
            DismissOnOutsideTap = false
        };

        var result = await _coachMarkService.ShowAsync(steps, options);
        StatusLabel.Text = $"Tour finished. Reason: {result.Reason}, Steps Completed: {result.LastStepIndex + 1}/{result.TotalSteps}";
    }

    private async void OnSingleMarkClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = "Showing single coach mark...";

        var step = new CoachMarkStep(
            target: SingleMarkBtn,
            title: "Single Feature Highlight",
            description: "You can highlight individual controls at any time.")
        {
            Placement = CoachMarkPlacement.Bottom,
            Shape = SpotlightShape.RoundedRectangle
        };

        var result = await _coachMarkService.ShowAsync(step, new CoachMarkOptions { DismissOnOutsideTap = true });
        StatusLabel.Text = $"Single mark finished. Reason: {result.Reason}";
    }

    private async void OnCancelableTourClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = "Starting tour with 3s CancellationToken...";
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));

        var steps = new List<CoachMarkStep>
        {
            new CoachMarkStep(SearchBox, "Search Bar", "This tour will auto-cancel in 3 seconds."),
            new CoachMarkStep(DeepActionButton, "Deep Action", "Step 2...")
        };

        var result = await _coachMarkService.ShowAsync(steps, cancellationToken: cts.Token);
        StatusLabel.Text = $"Tour result: Reason={result.Reason}, IsCanceled={result.IsCanceled}";
    }

    private async void OnNavAwayClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = "Starting tour and navigating away in 1.5s...";

        var steps = new List<CoachMarkStep>
        {
            new CoachMarkStep(StartTourBtn, "Navigation Demo", "Navigating away while active will safely tear down.")
        };

        _ = Task.Run(async () =>
        {
            await Task.Delay(1500);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Navigation.PushAsync(new DetailsPage());
            });
        });

        var result = await _coachMarkService.ShowAsync(steps);
        StatusLabel.Text = $"Tour result: Reason={result.Reason}";
    }

    private async void OnInspectItemClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn)
        {
            var itemName = (btn.BindingContext as SampleItem)?.Name ?? "CollectionView Item";
            var step = new CoachMarkStep(
                target: btn,
                title: itemName,
                description: $"Targeted '{itemName}' button at its exact live position in the CollectionView.")
            {
                Placement = CoachMarkPlacement.Left,
                Shape = SpotlightShape.RoundedRectangle,
                SpotlightCornerRadius = 8f
            };

            await _coachMarkService.ShowAsync(step);
        }
    }
}
