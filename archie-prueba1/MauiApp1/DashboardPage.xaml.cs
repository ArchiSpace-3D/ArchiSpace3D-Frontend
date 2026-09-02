namespace MauiApp1;

public partial class DashboardPage : ContentPage
{
    public DashboardPage()
    {
        InitializeComponent();
    }

    private async void OnEnterARClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new ARPage());
    }
}
