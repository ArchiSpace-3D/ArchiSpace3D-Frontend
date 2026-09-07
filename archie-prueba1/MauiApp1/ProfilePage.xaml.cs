using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1;

public partial class ProfilePage : ContentPage
{
    private UsuarioDto? _usuarioDto;

    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDatosUsuarioAsync();
    }

    private async Task CargarDatosUsuarioAsync()
    {
        NombreUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Nombre) ? "Usuario" : UserSession.Nombre;
        EmailUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Email) ? "Sin correo" : UserSession.Email;
        RolUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Rol) ? "Invitado" : UserSession.Rol;
        ServerUrlLabel.Text = UserSession.BaseUrl;
        SessionIdLabel.Text = $"ID de Usuario: {UserSession.Idusuario}";

        // Iniciales para el avatar
        string inicial = !string.IsNullOrEmpty(UserSession.Nombre) ? UserSession.Nombre.Substring(0, 1).ToUpper() : "A";
        AvatarInitialsLabel.Text = inicial;

        if (UserSession.IsAuthenticated)
        {
            _usuarioDto = await ApiService.GetUsuarioByIdAsync(UserSession.Idusuario);
            if (_usuarioDto != null)
            {
                NombreUsuarioLabel.Text = _usuarioDto.NombreCompleto;
                TelefonoLabel.Text = string.IsNullOrWhiteSpace(_usuarioDto.Telefono) ? "No especificado" : _usuarioDto.Telefono;
                DireccionLabel.Text = string.IsNullOrWhiteSpace(_usuarioDto.Direccion) ? "No especificada" : _usuarioDto.Direccion;
                DocumentoLabel.Text = string.IsNullOrWhiteSpace(_usuarioDto.Numerodocumento) ? "No especificado" : $"{_usuarioDto.Tipodocumento ?? "CC"}: {_usuarioDto.Numerodocumento}";
            }
        }
    }

    private async void OnOpenEditProfileSheetClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.94, 60); await btn.ScaleToAsync(1.0, 60); }
        if (!UserSession.IsAuthenticated)
        {
            await ShowToastAsync("Inicia sesión para editar tu perfil.");
            return;
        }

        if (_usuarioDto != null)
        {
            EditNombreEntry.Text = _usuarioDto.Nombre;
            EditApellidoEntry.Text = _usuarioDto.Apellido;
            EditTelefonoEntry.Text = _usuarioDto.Telefono;
            EditDireccionEntry.Text = _usuarioDto.Direccion;
            EditDocumentoEntry.Text = _usuarioDto.Numerodocumento;
        }
        else
        {
            EditNombreEntry.Text = UserSession.Nombre;
        }

        EditProfileSheetModal.IsVisible = true;
        EditProfileBackdrop.Opacity = 0;
        EditProfileSheetCard.TranslationY = 400;

        _ = EditProfileBackdrop.FadeToAsync(1.0, 250);
        await EditProfileSheetCard.TranslateToAsync(0, 0, 300, Easing.CubicOut);
    }

    private async void OnCloseEditProfileSheetClicked(object? sender, EventArgs e)
    {
        _ = EditProfileBackdrop.FadeToAsync(0, 200);
        await EditProfileSheetCard.TranslateToAsync(0, 400, 250, Easing.CubicIn);
        EditProfileSheetModal.IsVisible = false;
    }

    private async void OnGuardarPerfilClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
        if (!UserSession.IsAuthenticated) return;

        var request = new ActualizarUsuarioRequest
        {
            Idusuario = UserSession.Idusuario,
            Nombre = EditNombreEntry.Text?.Trim() ?? UserSession.Nombre,
            Apellido = EditApellidoEntry.Text?.Trim() ?? "",
            Email = UserSession.Email,
            Rol = UserSession.Rol,
            Telefono = EditTelefonoEntry.Text?.Trim(),
            Direccion = EditDireccionEntry.Text?.Trim(),
            Tipodocumento = "CC",
            Numerodocumento = EditDocumentoEntry.Text?.Trim()
        };

        var (success, message) = await ApiService.ActualizarUsuarioAsync(UserSession.Idusuario, request);
        OnCloseEditProfileSheetClicked(null, EventArgs.Empty);

        if (success)
        {
            UserSession.Nombre = request.Nombre;
            await CargarDatosUsuarioAsync();
            await ShowToastAsync("Perfil actualizado correctamente.");
        }
        else
        {
            await ShowToastAsync(message);
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }

        UserSession.ClearSession();
        App.SetRootPage(new NavigationPage(new LoginPage()));
    }

    private async Task ShowToastAsync(string message)
    {
        AppleToastMessage.Text = message;
        AppleToast.IsVisible = true;
        _ = AppleToast.FadeToAsync(1.0, 200);
        await AppleToast.TranslateToAsync(0, 10, 200, Easing.CubicOut);
        await Task.Delay(2000);
        _ = AppleToast.FadeToAsync(0, 200);
        await AppleToast.TranslateToAsync(0, 0, 200, Easing.CubicIn);
        AppleToast.IsVisible = false;
    }
}
