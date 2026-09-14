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

    private void OnTogglePasswordVisibility(object? sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        PasswordToggleIcon.Source = PasswordEntry.IsPassword ? "ic_eye_off.svg" : "ic_eye_on.svg";
    }

    private void OnToggleRegPasswordVisibility(object? sender, EventArgs e)
    {
        RegPasswordEntry.IsPassword = !RegPasswordEntry.IsPassword;
        RegPasswordToggleIcon.Source = RegPasswordEntry.IsPassword ? "ic_eye_off.svg" : "ic_eye_on.svg";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        string? savedEmail = Preferences.Get("saved_email", null);
        if (!string.IsNullOrEmpty(savedEmail))
        {
            EmailEntry.Text = savedEmail;
        }
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await LoginButton.ScaleToAsync(0.96, 70);
        await LoginButton.ScaleToAsync(1.0, 80);
        
        string hostOrIp = "https://archispace3d-backend-production.up.railway.app";
        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            await ShowToastAsync("Ingresa correo y contraseña.");
            return;
        }

        LoginButton.IsEnabled = false;
        LoadingOverlay.IsVisible = true;

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

    private async void OnOpenRegisterSheetClicked(object? sender, EventArgs e)
    {
        RegisterBackdrop.IsVisible = true;
        await RegisterBackdrop.FadeToAsync(1, 200);
        await RegisterSheetCard.TranslateToAsync(0, 0, 350, Easing.CubicOut);
    }

    private async void OnCloseRegisterSheetClicked(object? sender, EventArgs e)
    {
        await CloseRegisterSheet();
    }

    private async Task CloseRegisterSheet()
    {
        await RegisterSheetCard.TranslateToAsync(0, 1200, 250, Easing.CubicIn);
        await RegisterBackdrop.FadeToAsync(0, 200);
        RegisterBackdrop.IsVisible = false;
    }

    private async void OnSubmitRegisterClicked(object? sender, EventArgs e)
    {
        await BtnSubmitRegister.ScaleToAsync(0.95, 70);
        await BtnSubmitRegister.ScaleToAsync(1.0, 70);

        string hostOrIp = "https://archispace3d-backend-production.up.railway.app";
        string email = RegEmailEntry.Text?.Trim() ?? string.Empty;
        string pass = RegPasswordEntry.Text?.Trim() ?? string.Empty;
        string nom = RegNombreEntry.Text?.Trim() ?? string.Empty;
        string ape = RegApellidoEntry.Text?.Trim() ?? string.Empty;
        string rol = RegRolPicker.SelectedItem?.ToString() ?? "Cliente";
        
        
        
        

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(nom))
        {
            await ShowToastAsync("Llena todos los campos.");
            return;
        }

        BtnSubmitRegister.IsEnabled = false;
        LoadingOverlay.IsVisible = true;

        try
        {
            var usuario = new UsuarioRegistroRequest
            {
                Email = email,
                Contrasena = pass,
                Nombre = nom,
                Apellido = ape,
                Rol = rol,
                
                
                
                
            };

            var (success, message) = await ApiService.RegistrarUsuarioAsync(hostOrIp, usuario);
            LoadingOverlay.IsVisible = false;

            if (success)
            {
                EmailEntry.Text = email;
                await CloseRegisterSheet();
                await ShowToastAsync("Cuenta creada! Inicia sesión.");
            }
            else
            {
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
            BtnSubmitRegister.IsEnabled = true;
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
}

