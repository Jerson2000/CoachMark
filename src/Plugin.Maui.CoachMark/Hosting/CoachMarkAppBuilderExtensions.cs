namespace Plugin.Maui.CoachMark;

/// <summary>
/// Extension methods for configuring and registering CoachMark in .NET MAUI dependency injection.
/// </summary>
public static class CoachMarkAppBuilderExtensions
{
    /// <summary>
    /// Registers the CoachMark services and dependencies into the MauiApp service collection.
    /// </summary>
    public static MauiAppBuilder UseCoachMark(this MauiAppBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<ITooltipPlacementEngine, TooltipPlacementEngine>();
        builder.Services.AddSingleton<ICoordinateResolver, VisualTreeCoordinateResolver>();
        builder.Services.AddTransient<IElementTracker>(sp =>
        {
            var dispatcher = sp.GetRequiredService<IDispatcher>();
            return new ElementTracker(dispatcher);
        });

        builder.Services.AddSingleton<ICoachMarkService>(sp =>
        {
            var dispatcher = sp.GetService<IDispatcher>() ?? Application.Current?.Dispatcher ?? Dispatcher.GetForCurrentThread()!;
            var coordinateResolver = sp.GetRequiredService<ICoordinateResolver>();
            var placementEngine = sp.GetRequiredService<ITooltipPlacementEngine>();
            var elementTracker = sp.GetRequiredService<IElementTracker>();

            var controller = new CoachMarkController(dispatcher, coordinateResolver, placementEngine, elementTracker);
            return new CoachMarkService(controller);
        });

        return builder;
    }
}
