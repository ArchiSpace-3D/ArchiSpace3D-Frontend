using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1;

public partial class ProfilePage : ContentPage
{
    private UsuarioDto? _usuarioActual;

    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarDatosUsuarioAsync();
        
        ThemeSwitch.IsToggled = Application.Current!.UserAppTheme == AppTheme.Dark;
        ThemeIcon.Source = ThemeSwitch.IsToggled ? "ic_moon.svg" : "ic_sun.svg";
    }

    private void OnThemeSwitchToggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            Application.Current!.UserAppTheme = AppTheme.Dark;
            ThemeIcon.Source = "ic_moon.svg";
        }
        else
        {
            Application.Current!.UserAppTheme = AppTheme.Light;
            ThemeIcon.Source = "ic_sun.svg";
        }
    }

    private async void CargarDatosUsuarioAsync()
    {
        // Set basic data first from session
        NombreUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Nombre) ? "Usuario" : UserSession.Nombre;
        EmailUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Email) ? "--" : UserSession.Email;
        RolUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Rol) ? "Invitado" : UserSession.Rol;
        BtnGestionarUsuarios.IsVisible = (UserSession.Rol == "Arquitecto");

        string inicial = !string.IsNullOrEmpty(UserSession.Nombre) ? UserSession.Nombre.Substring(0, 1).ToUpper() : "A";
        AvatarInitialsLabel.Text = inicial;

        // Fetch full data from DB
        var user = await ApiService.GetUsuarioByIdAsync(UserSession.Idusuario);
        if (user != null)
        {
            _usuarioActual = user;
            NombreUsuarioLabel.Text = $"{user.Nombre} {user.Apellido}";
            EmailUsuarioLabel.Text = user.Email;
            RolUsuarioLabel.Text = user.Rol;
            
            TelefonoUsuarioLabel.Text = string.IsNullOrWhiteSpace(user.Telefono) ? "--" : user.Telefono;
            DireccionUsuarioLabel.Text = string.IsNullOrWhiteSpace(user.Direccion) ? "--" : user.Direccion;
            DocumentoUsuarioLabel.Text = string.IsNullOrWhiteSpace(user.Numerodocumento) ? "--" : user.Numerodocumento;
            
            // Sync Session
            UserSession.Nombre = user.Nombre;
            UserSession.Apellido = user.Apellido;
            UserSession.Telefono = user.Telefono;
            UserSession.Direccion = user.Direccion;
            UserSession.Numerodocumento = user.Numerodocumento;
            UserSession.Tipodocumento = user.Tipodocumento;
        }
    }

    private async void OnUploadAvatarClicked(object sender, EventArgs e)
    {
        await ShowAlertAsync("Próximamente", "La carga de imágenes de perfil estará disponible en la próxima actualización, usando Supabase Storage.", "Entendido");
    }

    private async void OnOpenEditProfileClicked(object sender, EventArgs e)
    {
        if (_usuarioActual != null)
        {
            EditNombreEntry.Text = _usuarioActual.Nombre;
            EditApellidoEntry.Text = _usuarioActual.Apellido;
            EditTelefonoEntry.Text = _usuarioActual.Telefono;
            EditDireccionEntry.Text = _usuarioActual.Direccion;
            EditDocumentoEntry.Text = _usuarioActual.Numerodocumento;
        }

        EditProfileBackdrop.IsVisible = true;
        await EditProfileBackdrop.FadeTo(1, 200);
        await EditProfileSheetCard.TranslateTo(0, 0, 350, Easing.CubicOut);
    }

    private async void OnCloseEditProfileClicked(object sender, EventArgs e)
    {
        await CloseEditProfileSheet();
    }

    private async Task CloseEditProfileSheet()
    {
        await EditProfileSheetCard.TranslateTo(0, 800, 250, Easing.CubicIn);
        await EditProfileBackdrop.FadeTo(0, 200);
        EditProfileBackdrop.IsVisible = false;
    }

    private async void OnSubmitEditProfileClicked(object sender, EventArgs e)
    {
        if (_usuarioActual == null) return;
        
        var req = new ActualizarUsuarioRequest
        {
            Nombre = EditNombreEntry.Text ?? _usuarioActual.Nombre,
            Apellido = EditApellidoEntry.Text ?? _usuarioActual.Apellido,
            Telefono = EditTelefonoEntry.Text,
            Direccion = EditDireccionEntry.Text,
            Numerodocumento = EditDocumentoEntry.Text,
            Tipodocumento = _usuarioActual.Tipodocumento,
            Rol = _usuarioActual.Rol
        };

        var (success, msg) = await ApiService.ActualizarUsuarioAsync(_usuarioActual.Idusuario, req);
        if (success)
        {
            await CloseEditProfileSheet();
            CargarDatosUsuarioAsync();
            await ShowAlertAsync("Éxito", "Perfil actualizado correctamente.", "OK");
        }
        else
        {
            await ShowAlertAsync("Error", msg, "OK");
        }
    }

    private async void OnGestionarUsuariosClicked(object sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
        await Navigation.PushModalAsync(new AdminUsersPage());
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }

        bool confirm = await Application.Current!.Windows[0].Page!.DisplayAlert("Cerrar Sesión", "¿Estás seguro que deseas salir?", "Sí, Salir", "Cancelar");
        if (!confirm) return;

        UserSession.ClearSession();
        Application.Current!.Windows[0].Page = new LoginPage();
    }

    private Task ShowAlertAsync(string title, string message, string cancel)
    {
        return Application.Current!.Windows[0].Page!.DisplayAlert(title, message, cancel);
    }
}
