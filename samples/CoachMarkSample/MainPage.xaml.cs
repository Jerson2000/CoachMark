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
    private readonly DemoCanvasDrawable _demoDrawable = new();
    public ObservableCollection<SampleItem> Items { get; } = new();

    public MainPage(ICoachMarkService coachMarkService)
    {
        InitializeComponent();
        _coachMarkService = coachMarkService ?? throw new ArgumentNullException(nameof(coachMarkService));

        DemoGraphicsView.Drawable = _demoDrawable;

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

    private async void OnShellAndRectTourClicked(object? sender, EventArgs e)
    {
        StatusLabel.Text = "Shell & Drawable Rect Tour started...";

        var steps = new List<CoachMarkStep>
        {
            new CoachMarkStep(
                shellTarget: ShellTarget.FlyoutIcon(),
                title: "Shell Flyout Menu",
                description: "Targets the native Shell hamburger menu button with Window-level overlay hosting.")
            {
                Placement = CoachMarkPlacement.Bottom,
                Shape = SpotlightShape.Circle,
                BeforeShowAsync = async () =>
                {
                    if (Shell.Current is { FlyoutIsPresented: true } shell)
                    {
                        shell.FlyoutIsPresented = false;
                        await Task.Delay(250);
                    }
                    return true;
                }
            },
            new CoachMarkStep(
                shellTarget: ShellTarget.FlyoutItem("Showcase"),
                title: "Sidebar: Showcase",
                description: "Opens the Shell sidebar drawer and highlights a navigation item inside it.")
            {
                Placement = CoachMarkPlacement.Right,
                Shape = SpotlightShape.RoundedRectangle,
                BeforeShowAsync = async () =>
                {
                    if (Shell.Current is { FlyoutIsPresented: false } shell)
                    {
                        shell.FlyoutIsPresented = true;
                        await Task.Delay(300);
                    }
                    return true;
                }
            },
            new CoachMarkStep(
                shellTarget: ShellTarget.FlyoutItem("About"),
                title: "Sidebar: About",
                description: "Highlights another sidebar menu item inside the open flyout drawer.")
            {
                Placement = CoachMarkPlacement.Right,
                Shape = SpotlightShape.RoundedRectangle,
                BeforeShowAsync = async () =>
                {
                    if (Shell.Current is { FlyoutIsPresented: false } shell)
                    {
                        shell.FlyoutIsPresented = true;
                        await Task.Delay(300);
                    }
                    return true;
                },
                AfterDismissAsync = async () =>
                {
                    if (Shell.Current is { FlyoutIsPresented: true } shell)
                    {
                        shell.FlyoutIsPresented = false;
                        await Task.Delay(250);
                    }
                }
            },
            new CoachMarkStep(
                shellTarget: ShellTarget.Tab(0),
                title: "Home Tab Icon",
                description: "Targets the first Shell tab bar icon by zero-based index.")
            {
                Placement = CoachMarkPlacement.Auto,
                Shape = SpotlightShape.RoundedRectangle,
                BeforeShowAsync = async () =>
                {
                    if (Shell.Current is { FlyoutIsPresented: true } shell)
                    {
                        shell.FlyoutIsPresented = false;
                        await Task.Delay(250);
                    }
                    return true;
                }
            },
            new CoachMarkStep(
                shellTarget: ShellTarget.Tab("Details"),
                title: "Details Tab Icon",
                description: "Targets the Details tab bar icon by title or route.")
            {
                Placement = CoachMarkPlacement.Auto,
                Shape = SpotlightShape.RoundedRectangle
            },
            new CoachMarkStep(
                shellTarget: ShellTarget.Tab("Explore"),
                title: "Explore Tab Icon",
                description: "Targets the Explore tab bar icon by title or route.")
            {
                Placement = CoachMarkPlacement.Auto,
                Shape = SpotlightShape.RoundedRectangle
            },
            new CoachMarkStep(
                relativeTo: DemoGraphicsView,
                rectAccessor: () => _demoDrawable.PrimaryNodeRect,
                title: "Drawable Card Region",
                description: "Targets a specific Rect drawn inside a GraphicsView IDrawable.")
            {
                Placement = CoachMarkPlacement.Top,
                Shape = SpotlightShape.RoundedRectangle,
                SpotlightCornerRadius = 10f
            },
            new CoachMarkStep(
                relativeTo: DemoGraphicsView,
                rectAccessor: () => _demoDrawable.SecondaryNodeRect,
                title: "Drawable Badge Circle",
                description: "Tracks a second dynamic drawable region inside the canvas.")
            {
                Placement = CoachMarkPlacement.Top,
                Shape = SpotlightShape.Circle
            }
        };

        var options = new CoachMarkOptions
        {
            OverlayHostMode = CoachMarkOverlayHostMode.Auto,
            OverlayOpacity = 0.75f,
            SpotlightBorderColor = Color.FromArgb("#818CF8"),
            SpotlightBorderWidth = 2f
        };

        var result = await _coachMarkService.ShowAsync(steps, options);
        if (Shell.Current is { FlyoutIsPresented: true } currentShell)
        {
            currentShell.FlyoutIsPresented = false;
        }
        StatusLabel.Text = $"Shell & Rect Tour finished. Reason: {result.Reason}, Steps: {result.LastStepIndex + 1}/{result.TotalSteps}";
    }

    private sealed class DemoCanvasDrawable : IDrawable
    {
        public Rect PrimaryNodeRect { get; private set; } = new(16, 20, 140, 100);
        public Rect SecondaryNodeRect { get; private set; } = new(210, 32, 76, 76);

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();

            canvas.FillColor = Color.FromArgb("#F1F5F9");
            canvas.FillRoundedRectangle(dirtyRect, 12);

            float cardWidth = Math.Min(160f, Math.Max(100f, dirtyRect.Width * 0.45f));
            var primary = new RectF(16f, 20f, cardWidth, Math.Max(48f, dirtyRect.Height - 40f));
            PrimaryNodeRect = new Rect(primary.X, primary.Y, primary.Width, primary.Height);

            canvas.FillColor = Color.FromArgb("#EEF2FF");
            canvas.StrokeColor = Color.FromArgb("#4F46E5");
            canvas.StrokeSize = 2f;
            canvas.FillRoundedRectangle(primary, 10f);
            canvas.DrawRoundedRectangle(primary, 10f);

            canvas.FontColor = Color.FromArgb("#312E81");
            canvas.FontSize = 13f;
            canvas.DrawString("Drawable Rect #1", primary, HorizontalAlignment.Center, VerticalAlignment.Center);

            float circleSize = 72f;
            float circleX = Math.Max(primary.Right + 24f, dirtyRect.Width - circleSize - 24f);
            float circleY = Math.Max(8f, (dirtyRect.Height - circleSize) / 2f);
            var secondary = new RectF(circleX, circleY, circleSize, circleSize);
            SecondaryNodeRect = new Rect(secondary.X, secondary.Y, secondary.Width, secondary.Height);

            canvas.FillColor = Color.FromArgb("#0D9488");
            canvas.FillEllipse(secondary);
            canvas.FontColor = Colors.White;
            canvas.DrawString("#2", secondary, HorizontalAlignment.Center, VerticalAlignment.Center);

            canvas.RestoreState();
        }
    }
}
