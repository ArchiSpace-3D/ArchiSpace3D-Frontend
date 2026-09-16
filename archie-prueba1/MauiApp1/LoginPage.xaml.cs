using MauiApp1.Models;
using MauiApp1.Services;
using System.Text.Json;

namespace MauiApp1;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        string? savedEmail = Preferences.Get("saved_email", null);
        if (!string.IsNullOrEmpty(savedEmail))
        {
            EmailEntry.Text = savedEmail;
        }
    }

    private void OnTogglePasswordVisibility(object? sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        PasswordToggleIcon.Source = PasswordEntry.IsPassword ? "ic_eye_off.svg" : "ic_eye_on.svg";
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
                    LoadingLabel.Text = "Despertando servidor de Railway,\nesto puede demorar hasta 30 seg...";
                });
            }
        });
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await LoginButton.ScaleToAsync(0.95, 70);
        await LoginButton.ScaleToAsync(1.0, 70);

        string hostOrIp = "https://archispace3d-backend-production.up.railway.app";
        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string pass = PasswordEntry.Text?.Trim() ?? string.Empty;
        string password = pass;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            await ShowToastAsync("Ingresa correo y contraseña.");
            return;
        }

        LoginButton.IsEnabled = false;
        ShowLoadingWithTimeout();

        try
        {
            var (success, message, response) = await ApiService.LoginAsync(hostOrIp, email, password);

            if (success && response != null)
            {
                Preferences.Set("saved_email", email);
                LoadingOverlay.IsVisible = false;
                Application.Current!.Windows[0].Page = new AppShell();
            }
            else
            {
                LoadingOverlay.IsVisible = false;
                await ShowToastAsync(message);
            }
        }
        catch (Exception ex)
        {
            LoadingOverlay.IsVisible = false;
            await ShowToastAsync($"Error: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private async void OnGoToRegisterClicked(object? sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 0)
        {
            await Navigation.PushAsync(new RegisterPage());
        }
        else
        {
            await Navigation.PushModalAsync(new NavigationPage(new RegisterPage()));
        }
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

    private async void OnGoogleLoginClicked(object? sender, EventArgs e)
    {
        try
        {
            LoadingOverlay.IsVisible = true;
            await SignalRService.DisconnectAsync();

            var authState = await SupabaseService.Client.Auth.SignIn(
                Supabase.Gotrue.Constants.Provider.Google,
                new Supabase.Gotrue.SignInOptions
                {
                    RedirectTo = "com.archispace.archie://login-callback"
                });

            var result = await WebAuthenticator.Default.AuthenticateAsync(
                new WebAuthenticatorOptions
                {
                    Url = new Uri(authState.Uri.ToString()),
                    CallbackUrl = new Uri("com.archispace.archie://login-callback")
                });

            if (result.Properties.TryGetValue("access_token", out var accessToken))
            {
                var (success, message, response) = await ApiService.GoogleLoginAsync(accessToken);
                LoadingOverlay.IsVisible = false;

                if (success)
                {
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
                await ShowToastAsync("No se pudo completar el inicio de sesión con Google.");
            }
        }
        catch (Exception ex)
        {
            LoadingOverlay.IsVisible = false;
            await ShowToastAsync($"Error con Google: {ex.Message}");
        }
    }

    private async void OnAppleLoginClicked(object? sender, EventArgs e)
    {
        await ShowToastAsync("Inicio con Apple — próximamente disponible.");
    }

    private async void OnFacebookLoginClicked(object? sender, EventArgs e)
    {
        await ShowToastAsync("Inicio con Facebook — próximamente disponible.");
    }
}