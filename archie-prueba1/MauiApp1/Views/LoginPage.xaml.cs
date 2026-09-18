using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using MauiApp1.Services;
using MauiApp1.Models;

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
        
        if (width > height) // Landscape
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
        else // Portrait
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
        PasswordToggleIcon.Source = PasswordEntry.IsPassword ? "ic_eye_off.svg" : "ic_eye_on.svg";
    }

    private async void OnGoogleLoginClicked(object? sender, EventArgs e)
    {
        await GoogleLoginButton.ScaleToAsync(0.95, 70);
        await GoogleLoginButton.ScaleToAsync(1.0, 70);

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

            var result = await Microsoft.Maui.Authentication.WebAuthenticator.Default.AuthenticateAsync(
                new Microsoft.Maui.Authentication.WebAuthenticatorOptions
                {
                    Url = new System.Uri(authState.Uri.ToString()),
                    CallbackUrl = new System.Uri("com.archispace.archie://login-callback")
                });

            if (result.Properties.TryGetValue("access_token", out var accessToken))
            {
                var (success, message, response) = await ApiService.GoogleLoginAsync(accessToken);
                LoadingOverlay.IsVisible = false;

                                if (success && response != null)
                {
                    _ = MauiApp1.Services.FirebasePushService.InicializarYRegistrarAsync();
                    Microsoft.Maui.Controls.Application.Current!.Windows[0].Page = new AppShell();
                }
                else
                {
                    await ShowToastAsync(message);
                }
            }
        }
        catch (System.Threading.Tasks.TaskCanceledException)
        {
            LoadingOverlay.IsVisible = false;
        }
        catch (System.Exception ex)
        {
            LoadingOverlay.IsVisible = false;
            await ShowToastAsync("Error Google Auth: " + ex.Message);
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await LoginButton.ScaleToAsync(0.95, 70);
        await LoginButton.ScaleToAsync(1.0, 70);

        string hostOrIp = "https://archispace3d-backend-production.up.railway.app";
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
            var (success, message, response) = await ApiService.LoginAsync(hostOrIp, email, password);

                            if (success && response != null)
                {
                    LoadingOverlay.IsVisible = false;
                    _ = MauiApp1.Services.FirebasePushService.InicializarYRegistrarAsync();
                    Microsoft.Maui.Controls.Application.Current!.Windows[0].Page = new AppShell();
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




