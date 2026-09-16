using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1.Views;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        CargarDatosUsuarioUI();
        
        await Task.WhenAll(
            MainScroll.FadeTo(1, 600, Easing.CubicOut),
            MainScroll.TranslateTo(0, 0, 600, Easing.CubicOut)
        );
    }

    private void CargarDatosUsuarioUI()
    {
        NombreUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Nombre) ? "Usuario" : $"{UserSession.Nombre} {UserSession.Apellido}";
        EmailUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Email) ? "Sin correo" : UserSession.Email;
        RolUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Rol) ? "Invitado" : UserSession.Rol;
        
        TelefonoUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Telefono) ? "No registrado" : UserSession.Telefono;
        DireccionUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Direccion) ? "No registrada" : UserSession.Direccion;
        DocumentoUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Numerodocumento) ? "No registrado" : UserSession.Numerodocumento;
        
        BtnGestiónarUsuarios.IsVisible = (UserSession.Rol == "Arquitecto");

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
            Idusuario = UserSession.Idusuario,
            Nombre = EditNombreEntry.Text ?? "",
            Apellido = EditApellidoEntry.Text ?? "",
            Email = UserSession.Email ?? "",
            Contrasena = "dummy_password", // To pass backend [Required] validation (ignored in Dao)
            Rol = UserSession.Rol ?? "Invitado",
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
                
                using var streamForUi = await photo.OpenReadAsync();
                var memoryStreamUi = new MemoryStream();
                await streamForUi.CopyToAsync(memoryStreamUi);
                memoryStreamUi.Position = 0;
                ProfileImage.Source = ImageSource.FromStream(() => memoryStreamUi);

                // Convert file to byte array for upload
                using var memoryStream = new MemoryStream();
                using var stream = await photo.OpenReadAsync();
                await stream.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                // Send the image to the C# Backend to handle everything!
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", UserSession.Token);
                
                using var content = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(imageBytes);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(fileContent, "file", photo.FileName);

                string backendUrl = $"{UserSession.BaseUrl}/api/usuario/{UserSession.Idusuario}/avatar";
                var response = await httpClient.PostAsync(backendUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(resultJson);
                    string publicUrl = jsonDoc.RootElement.GetProperty("url").GetString()!;

                    UserSession.Avatarurl = publicUrl;
                    await SecureStorage.SetAsync("user_avatarurl", publicUrl);
                    await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Éxito", "Foto de perfil actualizada correctamente a través del servidor.", "OK");
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Error", $"No se pudo guardar en el servidor.\nDetalle: {error}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"No se pudo subir la foto: {ex.Message}", "OK");
        }
    }

    private async void OnGestiónarUsuariosClicked(object? sender, EventArgs e)
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




