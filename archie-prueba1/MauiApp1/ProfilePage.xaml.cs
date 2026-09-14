using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarDatosUsuarioUI();
    }

    private void CargarDatosUsuarioUI()
    {
        NombreUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Nombre) ? "Usuario" : $"{UserSession.Nombre} {UserSession.Apellido}";
        EmailUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Email) ? "Sin correo" : UserSession.Email;
        RolUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Rol) ? "Invitado" : UserSession.Rol;
        
        TelefonoUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Telefono) ? "No registrado" : UserSession.Telefono;
        DireccionUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Direccion) ? "No registrada" : UserSession.Direccion;
        DocumentoUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Numerodocumento) ? "No registrado" : UserSession.Numerodocumento;
        
        BtnGestionarUsuarios.IsVisible = (UserSession.Rol == "Arquitecto");

        string inicial = !string.IsNullOrEmpty(UserSession.Nombre) ? UserSession.Nombre.Substring(0, 1).ToUpper() : "A";
        AvatarInitialsLabel.Text = inicial;
        if (!string.IsNullOrEmpty(UserSession.Avatarurl))
        {
            AvatarInitialsLabel.IsVisible = false;
            ProfileImage.IsVisible = true;
            ProfileImage.Source = UserSession.Avatarurl;
        }
        else
        {
            AvatarInitialsLabel.IsVisible = true;
            ProfileImage.IsVisible = false;
        }
    }

    private async void OnToggleThemeClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.9, 100); await btn.ScaleToAsync(1.0, 100); }
        
        if (Application.Current!.UserAppTheme == AppTheme.Dark)
        {
            Application.Current.UserAppTheme = AppTheme.Light;
            ThemeIconImage.Source = "ic_moon.svg";
        }
        else
        {
            Application.Current.UserAppTheme = AppTheme.Dark;
            ThemeIconImage.Source = "ic_sun.svg";
        }
    }

    private async void OnEditProfileClicked(object sender, EventArgs e)
    {
        EditNombreEntry.Text = UserSession.Nombre;
        EditApellidoEntry.Text = UserSession.Apellido;
        EditTelefonoEntry.Text = UserSession.Telefono;
        EditDireccionEntry.Text = UserSession.Direccion;
        EditDocumentoEntry.Text = UserSession.Numerodocumento;

        EditProfileBackdrop.IsVisible = true;
        await EditProfileBackdrop.FadeToAsync(1, 200);
        await EditProfileSheetModal.TranslateToAsync(0, 0, 350, Easing.CubicOut);
    }

    private async void OnCloseEditProfileSheetClicked(object sender, EventArgs e)
    {
        await CloseEditProfileSheet();
    }

    private async Task CloseEditProfileSheet()
    {
        await EditProfileSheetModal.TranslateToAsync(0, 800, 250, Easing.CubicIn);
        await EditProfileBackdrop.FadeToAsync(0, 200);
        EditProfileBackdrop.IsVisible = false;
    }

    private async void OnSubmitEditProfileClicked(object sender, EventArgs e)
    {
        var req = new ActualizarUsuarioRequest
        {
            Nombre = EditNombreEntry.Text ?? "",
            Apellido = EditApellidoEntry.Text ?? "",
            Telefono = EditTelefonoEntry.Text,
            Direccion = EditDireccionEntry.Text,
            Numerodocumento = EditDocumentoEntry.Text
        };

        var res = await ApiService.ActualizarUsuarioAsync(UserSession.Idusuario, req);
        if (res.Success)
        {
            UserSession.Nombre = req.Nombre;
            UserSession.Apellido = req.Apellido;
            UserSession.Telefono = req.Telefono;
            UserSession.Direccion = req.Direccion;
            UserSession.Numerodocumento = req.Numerodocumento;
            
            CargarDatosUsuarioUI();
            await CloseEditProfileSheet();
        }
        else
        {
            await ShowAlertAsync("Error", res.Message, "OK");
        }
    }

    private async void OnUploadProfilePictureClicked(object sender, EventArgs e)
    {
        try
        {
            var photo = await MediaPicker.Default.PickPhotoAsync(new MediaPickerOptions { Title = "Selecciona una foto de perfil" });
            if (photo != null)
            {
                // UI temporal mientras sube
                AvatarInitialsLabel.IsVisible = false;
                ProfileImage.IsVisible = true;
                var localStream = await photo.OpenReadAsync();
                ProfileImage.Source = ImageSource.FromStream(() => localStream);

                string url = "https://ejxfilcbchzhmbblrvve.supabase.co";
                // ADVERTENCIA: Se debe usar la clave anónima (publishable) y configurar RLS en Supabase 
                // para permitir la subida a 'avatars', ya que GitHub bloquea el commit de claves secretas.
                string key = "sb_publishable_-QBl6SvetxzzlVM88lpF_A_kXG8Lhc3"; 
                
                var options = new Supabase.SupabaseOptions { AutoConnectRealtime = false };
                var supabase = new Supabase.Client(url, key, options);
                await supabase.InitializeAsync();

                // Convert file to byte array
                using var memoryStream = new MemoryStream();
                using var stream = await photo.OpenReadAsync();
                await stream.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                string fileName = $"avatar_{UserSession.Idusuario}_{DateTime.Now.Ticks}.jpg";
                
                // Upload to Supabase Storage
                await supabase.Storage.From("avatars").Upload(imageBytes, fileName, new Supabase.Storage.FileOptions { CacheControl = "3600", Upsert = true });
                
                // Get public URL
                string publicUrl = supabase.Storage.From("avatars").GetPublicUrl(fileName);

                // Usar ApiService para actualizar el perfil mediante el backend
                var updateReq = new MauiApp1.Models.ActualizarUsuarioRequest
                {
                    Idusuario = UserSession.Idusuario,
                    Nombre = UserSession.Nombre,
                    Apellido = UserSession.Apellido,
                    Email = UserSession.Email,
                    Telefono = UserSession.Telefono,
                    Direccion = UserSession.Direccion,
                    Tipodocumento = UserSession.Tipodocumento,
                    Numerodocumento = UserSession.Numerodocumento,
                    Avatarurl = publicUrl
                };

                var response = await ApiService.ActualizarUsuarioAsync(UserSession.Idusuario, updateReq);
                
                if (response.Success)
                {
                    UserSession.Avatarurl = publicUrl;
                    await SecureStorage.SetAsync("user_avatarurl", publicUrl);
                    await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Éxito", "Foto de perfil actualizada correctamente en el servidor.", "OK");
                }
                else
                {
                    await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Advertencia", $"La foto se subió, pero hubo un error al guardarla en el servidor.\nDetalle: {response.Message}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo subir la foto: {ex.Message}", "OK");
        }
    }

    private async void OnGestionarUsuariosClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
        await Navigation.PushModalAsync(new AdminUsersPage());
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }

        bool confirm = await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Cerrar Sesión", "¿Estás seguro que deseas salir?", "Sí, Salir", "Cancelar");
        if (!confirm) return;

        UserSession.ClearSession();
        Application.Current!.Windows[0].Page = new LoginPage();
    }

    private Task ShowAlertAsync(string title, string message, string cancel)
    {
        return Application.Current!.Windows[0].Page!.DisplayAlertAsync(title, message, cancel);
    }
}




