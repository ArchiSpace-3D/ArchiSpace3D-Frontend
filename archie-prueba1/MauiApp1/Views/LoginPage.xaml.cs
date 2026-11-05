using System;
using System.Threading.Tasks;
using MauiApp1.Services;
using Microsoft.Maui.Controls;

namespace MauiApp1.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width > height)
        {
            RootGrid.ColumnDefinitions.Clear();
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4.5, GridUnitType.Star) });
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5.5, GridUnitType.Star) });

            Grid.SetColumn(BgImage, 1);
            Grid.SetColumnSpan(BgImage, 1);

            Grid.SetColumn(FormCard, 0);
            Grid.SetColumnSpan(FormCard, 1);
            FormCard.Margin = new Thickness(0);
            FormCard.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(0) };
        }
        else
        {
            RootGrid.ColumnDefinitions.Clear();
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Grid.SetColumn(BgImage, 0);
            Grid.SetColumnSpan(BgImage, 1);

            Grid.SetColumn(FormCard, 0);
            Grid.SetColumnSpan(FormCard, 1);
            FormCard.Margin = new Thickness(24, 0, 24, 0);
            FormCard.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(30) };
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _ = ApiService.WarmUpServerAsync();

        MainLayout.Opacity = 0;
        MainLayout.TranslationY = 30;
        await Task.WhenAll(
            MainLayout.FadeToAsync(1, 600, Easing.CubicOut),
            MainLayout.TranslateToAsync(0, 0, 600, Easing.CubicOut)
        );
    }

    private void OnTogglePasswordVisibility(object? sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        PasswordToggleIcon.Source = PasswordEntry.IsPassword
            ? (isDark ? "ic_eye_off_white.svg" : "ic_eye_off.svg")
            : (isDark ? "ic_eye_on_white.svg" : "ic_eye_on.svg");
    }

    private async void OnForgotPasswordTapped(object? sender, EventArgs e)
    {
        await AlertService.ShowAlertAsync(
            "Recuperación de acceso",
            "Si registraste tu cuenta con Google, usa el botón de Google para ingresar directamente. Si necesitas restablecer tu contraseña, contacta a tu arquitecto o administrador del proyecto.",
            "Entendido");
    }

    private async void OnGoogleLoginClicked(object? sender, EventArgs e)
    {
        await GoogleLoginButton.ScaleToAsync(0.95, 70);
        await GoogleLoginButton.ScaleToAsync(1.0, 70);

        try
        {
            LoadingLabel.Text = "Conectando con Google...";
            LoadingOverlay.IsVisible = true;
            await SignalRService.DisconnectAsync();

            var authState = await SupabaseService.Client.Auth.SignIn(
                Supabase.Gotrue.Constants.Provider.Google,
                new Supabase.Gotrue.SignInOptions
                {
                    RedirectTo = "com.archispace.archie://login-callback"
                });

            if (authState?.Uri == null)
            {
                LoadingOverlay.IsVisible = false;
                await ShowToastAsync("No se pudo iniciar la autenticación con Google.");
                return;
            }

            var result = await Microsoft.Maui.Authentication.WebAuthenticator.Default.AuthenticateAsync(
                new Microsoft.Maui.Authentication.WebAuthenticatorOptions
                {
                    Url = new Uri(authState.Uri.ToString()),
                    CallbackUrl = new Uri("com.archispace.archie://login-callback")
                });

            if (result.Properties.TryGetValue("access_token", out var accessToken) && !string.IsNullOrWhiteSpace(accessToken))
            {
                var (success, message, response) = await ApiService.GoogleLoginAsync(accessToken);
                LoadingOverlay.IsVisible = false;

                if (success && response != null)
                {
                    _ = SignalRService.ConnectAsync();
                    _ = FirebasePushService.InicializarYRegistrarAsync();
                    Application.Current!.Windows[0].Page = new AppShell();
                }
                else
                {
                    await ShowToastAsync(message);
                }
            }
            else
            {
                LoadingOverlay.IsVisible = false;
                await ShowToastAsync("No se recibió token de acceso de Google.");
            }
        }
        catch (TaskCanceledException)
        {
            LoadingOverlay.IsVisible = false;
        }
        catch (Exception ex)
        {
            LoadingOverlay.IsVisible = false;
            await ShowToastAsync("Error Google Auth: " + ex.Message);
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await LoginButton.ScaleToAsync(0.95, 70);
        await LoginButton.ScaleToAsync(1.0, 70);

        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            await ShowToastAsync("Ingresa correo y contraseña.");
            return;
        }

        LoginButton.IsEnabled = false;
        ShowLoadingWithTimeout();

        try
        {
            var (success, message, response) = await ApiService.LoginAsync(UserSession.BaseUrl, email, password);

            if (success && response != null)
            {
                LoadingOverlay.IsVisible = false;
                _ = SignalRService.ConnectAsync();
                _ = FirebasePushService.InicializarYRegistrarAsync();
                Application.Current!.Windows[0].Page = new AppShell();
            }
            else
            {
                LoadingOverlay.IsVisible = false;
                await ShowToastAsync(message);
            }
        }
        catch (Exception)
        {
            LoadingOverlay.IsVisible = false;
            await ShowToastAsync("Error de conexión");
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private void ShowLoadingWithTimeout()
    {
        LoadingLabel.Text = "Conectando...";
        LoadingOverlay.IsVisible = true;
        _ = Task.Delay(4000).ContinueWith(t =>
        {
            if (LoadingOverlay.IsVisible)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    LoadingLabel.Text = "Despertando servidor,\nesto puede tomar hasta 30s...";
                });
            }
        });
    }

    private async void OnOpenRegisterSheetClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new RegisterPage());
    }

    private async Task ShowToastAsync(string message)
    {
        AppleToastMessage.Text = message;
        AppleToast.IsVisible = true;
        await AppleToast.FadeToAsync(1, 300);
        await Task.Delay(3000);
        await AppleToast.FadeToAsync(0, 300);
        AppleToast.IsVisible = false;
    }
}
