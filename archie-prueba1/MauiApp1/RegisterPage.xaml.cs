using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1;

public partial class RegisterPage : ContentPage
{
    private const string HostOrIp = "https://archispace3d-backend-production.up.railway.app";

    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
        }
        else
        {
            await Navigation.PopModalAsync();
        }
    }

    private void OnToggleRegPasswordVisibility(object sender, EventArgs e)
    {
        RegPasswordEntry.IsPassword = !RegPasswordEntry.IsPassword;
        RegPasswordToggleIcon.Source = RegPasswordEntry.IsPassword ? "ic_eye_off.svg" : "ic_eye_on.svg";
    }

    private void ShowLoadingWithTimeout()
    {
        LoadingLabel.Text = "Creando cuenta...";
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

    private async void OnSubmitRegisterClicked(object sender, EventArgs e)
    {
        await BtnSubmitRegister.ScaleToAsync(0.95, 70);
        await BtnSubmitRegister.ScaleToAsync(1.0, 70);

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

        if (!email.Contains('@') || !email.Contains('.'))
        {
            await ShowToastAsync("Ingresa un correo electrónico válido.");
            return;
        }

        if (pass.Length < 6)
        {
            await ShowToastAsync("La contraseña debe tener al menos 6 caracteres.");
            return;
        }

        BtnSubmitRegister.IsEnabled = false;
        ShowLoadingWithTimeout();

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

            var (success, message) = await ApiService.RegistrarUsuarioAsync(HostOrIp, usuario);
            LoadingOverlay.IsVisible = false;

            if (success)
            {
                Preferences.Set("saved_email", email);
                await ShowToastAsync("Cuenta creada! Inicia sesión.");

                if (Navigation.NavigationStack.Count > 1)
                {
                    await Navigation.PopAsync();
                }
                else
                {
                    await Navigation.PopModalAsync();
                }
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
        await DisplayAlertAsync("ArchiSpace", message, "OK");
    }
}