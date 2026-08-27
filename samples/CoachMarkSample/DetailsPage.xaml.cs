namespace CoachMarkSample;

public partial class DetailsPage : ContentPage
{
    public DetailsPage()
    {
        InitializeComponent();
    }

    private async void OnGoBackClicked(object? sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
