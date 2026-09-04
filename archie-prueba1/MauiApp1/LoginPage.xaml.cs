using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();

        // Si es emulador usa 10.0.2.2, si es teléfono físico o Windows usa la IP local del PC por Wi-Fi (192.168.101.75)
        string defaultIp = DeviceInfo.DeviceType == DeviceType.Virtual ? "10.0.2.2" : "192.168.101.75";
        IpEntry.Text = Preferences.Get("saved_ip", defaultIp);
        EmailEntry.Text = Preferences.Get("saved_email", string.Empty);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Estado inicial para la animación de entrada
        HeaderBlock.Opacity = 0;
        HeaderBlock.TranslationY = -30;
        LoginCard.Opacity = 0;
        LoginCard.TranslationY = 60;

        // Animación suave de entrada coordinada
        _ = HeaderBlock.FadeToAsync(1, 400, Easing.CubicOut);
        _ = HeaderBlock.TranslateToAsync(0, 0, 450, Easing.CubicOut);
        await Task.Delay(100);
        _ = LoginCard.FadeToAsync(1, 500, Easing.CubicOut);
        await LoginCard.TranslateToAsync(0, 0, 500, Easing.CubicOut);
    }

    private async void OnUseMyPcIpClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { _ = btn.ScaleToAsync(0.92, 60).ContinueWith(_ => btn.ScaleToAsync(1.0, 60)); }
        IpEntry.Text = "192.168.101.75";
    }

    private async void OnUseEmulatorIpClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { _ = btn.ScaleToAsync(0.92, 60).ContinueWith(_ => btn.ScaleToAsync(1.0, 60)); }
        IpEntry.Text = "10.0.2.2";
    }

    private async void OnUseLocalhostClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { _ = btn.ScaleToAsync(0.92, 60).ContinueWith(_ => btn.ScaleToAsync(1.0, 60)); }
        IpEntry.Text = "localhost";
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        await LoginButton.ScaleToAsync(0.96, 70);
        await LoginButton.ScaleToAsync(1.0, 80);
        string hostOrIp = IpEntry.Text?.Trim() ?? string.Empty;
        string email = EmailEntry.Text?.Trim() ?? string.Empty;
        string password = PasswordEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(hostOrIp) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            await ShowToastAsync("Ingresa la IP, correo y contraseña.");
            return;
        }

        LoginButton.IsEnabled = false;
        LoginButton.Text = "Conectando con Backend...";

        try
        {
            var (success, message, response) = await ApiService.LoginAsync(hostOrIp, email, password);

            if (success && response != null)
            {
                Preferences.Set("saved_ip", hostOrIp);
                Preferences.Set("saved_email", email);

                await ShowToastAsync($"¡Bienvenido {response.Nombre}!");
                await Task.Delay(400);

                App.SetRootPage(new AppShell());
            }
            else
            {
                await ShowToastAsync(message);
            }
        }
        catch (Exception ex)
        {
            await ShowToastAsync($"Error: {ex.Message}");
        }
        finally
        {
            LoginButton.IsEnabled = true;
            LoginButton.Text = "Iniciar Sesión";
        }
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        RegisterSheetModal.IsVisible = true;
        RegisterBackdrop.Opacity = 0;
        RegisterSheetCard.TranslationY = 450;

        _ = RegisterBackdrop.FadeToAsync(1.0, 250);
        await RegisterSheetCard.TranslateToAsync(0, 0, 300, Easing.CubicOut);
    }

    private async void OnCloseRegisterSheetClicked(object? sender, EventArgs e)
    {
        _ = RegisterBackdrop.FadeToAsync(0, 200);
        await RegisterSheetCard.TranslateToAsync(0, 450, 250, Easing.CubicIn);
        RegisterSheetModal.IsVisible = false;
    }

    private async void OnSubmitRegisterClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }

        string hostOrIp = IpEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(hostOrIp))
        {
            await ShowToastAsync("Ingresa la IP o Host primero.");
            return;
        }

        string nombre = RegNombreEntry.Text?.Trim() ?? "";
        string apellido = RegApellidoEntry.Text?.Trim() ?? "";
        string email = RegEmailEntry.Text?.Trim() ?? "";
        string contrasena = RegPasswordEntry.Text?.Trim() ?? "";
        string rol = RegRolPicker.SelectedItem?.ToString() ?? "Arquitecto";

        if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(contrasena))
        {
            await ShowToastAsync("Por favor completa los campos requeridos.");
            return;
        }

        var nuevoUsuario = new UsuarioRegistroRequest
        {
            Nombre = nombre,
            Apellido = apellido,
            Email = email,
            Contrasena = contrasena,
            Rol = rol
        };

        var (success, message) = await ApiService.RegistrarUsuarioAsync(hostOrIp, nuevoUsuario);

        if (success)
        {
            OnCloseRegisterSheetClicked(null, EventArgs.Empty);
            EmailEntry.Text = nuevoUsuario.Email;
            PasswordEntry.Text = nuevoUsuario.Contrasena;
            await ShowToastAsync("¡Registro exitoso! Ya puedes iniciar sesión.");
        }
        else
        {
            await ShowToastAsync(message);
        }
    }

    private void OnSkipClicked(object? sender, EventArgs e)
    {
        UserSession.ClearSession();
        App.SetRootPage(new AppShell());
    }

    private async Task ShowToastAsync(string message)
    {
        AppleToastMessage.Text = message;
        AppleToast.IsVisible = true;
        _ = AppleToast.FadeToAsync(1.0, 200);
        await AppleToast.TranslateToAsync(0, 10, 200, Easing.CubicOut);
        await Task.Delay(2500);
        _ = AppleToast.FadeToAsync(0, 200);
        await AppleToast.TranslateToAsync(0, 0, 200, Easing.CubicIn);
        AppleToast.IsVisible = false;
    }
}
