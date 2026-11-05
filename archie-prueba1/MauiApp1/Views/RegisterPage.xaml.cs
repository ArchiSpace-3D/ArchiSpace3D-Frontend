using System;
using System.Threading.Tasks;
using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Maui.Controls;

namespace MauiApp1.Views
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage()
        {
            InitializeComponent();
            RegRolPicker.SelectedIndex = 0; // Cliente por defecto
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
            MainLayout.Opacity = 0;
            MainLayout.TranslationY = 30;
            await Task.WhenAll(
                MainLayout.FadeToAsync(1, 600, Easing.CubicOut),
                MainLayout.TranslateToAsync(0, 0, 600, Easing.CubicOut)
            );
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private void OnToggleRegPasswordVisibility(object? sender, EventArgs e)
        {
            RegPasswordEntry.IsPassword = !RegPasswordEntry.IsPassword;
            bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
            RegPasswordToggleIcon.Source = RegPasswordEntry.IsPassword
                ? (isDark ? "ic_eye_off_white.svg" : "ic_eye_off.svg")
                : (isDark ? "ic_eye_on_white.svg" : "ic_eye_on.svg");
        }

        private async void OnSubmitRegisterClicked(object? sender, EventArgs e)
        {
            string nombre = RegNombreEntry.Text?.Trim() ?? string.Empty;
            string apellido = RegApellidoEntry.Text?.Trim() ?? string.Empty;
            string email = RegEmailEntry.Text?.Trim() ?? string.Empty;
            string password = RegPasswordEntry.Text?.Trim() ?? string.Empty;
            string? rol = RegRolPicker.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellido))
            {
                await AlertService.ShowAlertAsync("Datos incompletos", "Ingresa tu nombre y apellido.", "Entendido");
                return;
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || !email.Contains('.'))
            {
                await AlertService.ShowAlertAsync("Correo inválido", "Ingresa una dirección de correo electrónico válida.", "Entendido");
                return;
            }

            if (password.Length < 6)
            {
                await AlertService.ShowAlertAsync("Contraseña corta", "La contraseña debe tener al menos 6 caracteres.", "Entendido");
                return;
            }

            if (string.IsNullOrWhiteSpace(rol))
            {
                await AlertService.ShowAlertAsync("Rol requerido", "Selecciona si te registras como Cliente o Arquitecto.", "Entendido");
                return;
            }

            BtnSubmitRegister.IsEnabled = false;
            BtnSubmitRegister.Text = "Creando cuenta...";

            try
            {
                var nuevoUsuario = new UsuarioRegistroRequest
                {
                    Nombre = nombre,
                    Apellido = apellido,
                    Email = email,
                    Contrasena = password,
                    Rol = rol
                };

                var (regOk, regMsg) = await ApiService.RegistrarUsuarioAsync(UserSession.BaseUrl, nuevoUsuario);
                if (!regOk)
                {
                    await AlertService.ShowAlertAsync("No se pudo registrar", regMsg, "Cerrar");
                    return;
                }

                // Iniciar sesión automáticamente tras el registro exitoso
                var (loginOk, _, loginData) = await ApiService.LoginAsync(UserSession.BaseUrl, email, password);
                if (loginOk && loginData != null)
                {
                    _ = SignalRService.ConnectAsync();
                    _ = FirebasePushService.InicializarYRegistrarAsync();
                    Application.Current!.Windows[0].Page = new AppShell();
                    return;
                }

                await AlertService.ShowAlertAsync("Cuenta creada", "Tu cuenta fue registrada con éxito. Ahora puedes iniciar sesión.", "Continuar");
                await Navigation.PopModalAsync();
            }
            catch (Exception ex)
            {
                await AlertService.ShowAlertAsync("Error de conexión", $"No fue posible completar el registro: {ex.Message}", "Cerrar");
            }
            finally
            {
                BtnSubmitRegister.IsEnabled = true;
                BtnSubmitRegister.Text = "Crear Cuenta";
            }
        }
    }
}
