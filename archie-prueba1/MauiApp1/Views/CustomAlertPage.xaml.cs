namespace MauiApp1.Views;

public partial class CustomAlertPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _tcs;

    public CustomAlertPage(string title, string message, string acceptText, string? cancelText, TaskCompletionSource<bool> tcs)
    {
        InitializeComponent();
        _tcs = tcs;

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        ActionButton.Text = acceptText;

        string t = title.ToLowerInvariant();

        if (t.Contains("error") || t.Contains("fallo") || t.Contains("excep") || t.Contains("incorrect") || t.Contains("denegad"))
        {
            IconBg.BackgroundColor = Color.FromArgb("#FEE2E2");
            IconImage.Source = "ic_error.svg";

            ActionButton.BackgroundColor = Color.FromArgb("#FEE2E2");
            ActionButton.TextColor = Color.FromArgb("#991B1B");
        }
        else if (t.Contains("aviso") || t.Contains("advertencia") || t.Contains("sesi") || t.Contains("seguro") || t.Contains("eliminar"))
        {
            IconBg.BackgroundColor = Color.FromArgb("#FEF3C7");
            IconImage.Source = "ic_warning.svg";

            ActionButton.BackgroundColor = Color.FromArgb("#FEF3C7");
            ActionButton.TextColor = Color.FromArgb("#92400E");
        }
        else
        {
            IconBg.BackgroundColor = Color.FromArgb("#D1FAE5");
            IconImage.Source = "ic_check.svg";

            ActionButton.BackgroundColor = Color.FromArgb("#D1FAE5");
            ActionButton.TextColor = Color.FromArgb("#064E3B");
        }

        if (!string.IsNullOrEmpty(cancelText))
        {
            CancelButton.Text = cancelText;
            CancelButton.IsVisible = true;
            Grid.SetColumnSpan(ActionButton, 1);
            Grid.SetColumn(ActionButton, 1);
        }
    }

    private async void OnDismissClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync(false);
        _tcs.TrySetResult(true);
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync(false);
        _tcs.TrySetResult(false);
    }

    protected override bool OnBackButtonPressed()
    {
        _ = Navigation.PopModalAsync(false);
        _tcs.TrySetResult(false);
        return true;
    }
}
