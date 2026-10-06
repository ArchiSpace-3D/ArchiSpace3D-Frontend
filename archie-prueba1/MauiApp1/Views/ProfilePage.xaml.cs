using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace MauiApp1.Views;

public partial class ProfilePage : ContentPage
{
    private readonly ObservableCollection<ProyectoDto> _proyectos = new();
    private readonly ObservableCollection<MediciónDto> _mediciones = new();
    private readonly ObservableCollection<ProyectoDto> _salas = new();
    private readonly ObservableCollection<SugerenciaDto> _sugerencias = new();

    public ProfilePage()
    {
        InitializeComponent();

        SuggestionsListCollectionView.ItemsSource = _sugerencias;
        MeasurementsListCollectionView.ItemsSource = _mediciones;
        RoomsListCollectionView.ItemsSource = _salas;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        CargarDatosUsuarioUI();
        await CargarDatosAsync();

        await Task.WhenAll(
            MainScroll.FadeToAsync(1, 600, Easing.CubicOut),
            MainScroll.TranslateToAsync(0, 0, 600, Easing.CubicOut)
        );
    }

    private void CargarDatosUsuarioUI()
    {
        string nombreCompleto = string.IsNullOrWhiteSpace(UserSession.Apellido)
            ? UserSession.Nombre
            : $"{UserSession.Nombre} {UserSession.Apellido}";

        NombreUsuarioLabel.Text = string.IsNullOrWhiteSpace(nombreCompleto) ? "Usuario" : nombreCompleto;

        string inicial = !string.IsNullOrEmpty(UserSession.Nombre)
            ? UserSession.Nombre.Substring(0, 1).ToUpper()
            : "U";
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

    private async Task CargarDatosAsync()
    {
        try
        {
            bool esArquitecto = UserSession.Rol == "Arquitecto";

            var proyectos = esArquitecto
                ? await ApiService.GetProyectosByArquitectoAsync(UserSession.Idusuario)
                : await ApiService.GetProyectosByClienteAsync(UserSession.Idusuario);

            _proyectos.Clear();
            foreach (var p in proyectos) _proyectos.Add(p);

            // Mediciones del proyecto activo
            _mediciones.Clear();
            if (UserSession.ActiveProject != null)
            {
                var mediciones = await ApiService.GetMediciónesByProyectoAsync(UserSession.ActiveProject.Idproyecto);
                foreach (var m in mediciones) _mediciones.Add(m);
            }

            // Salas = proyectos con código de sala activa
            _salas.Clear();
            foreach (var p in proyectos.Where(x => !string.IsNullOrWhiteSpace(x.Codigosalaactiva)))
                _salas.Add(p);

            // Sugerencias de TODOS los proyectos del usuario
            _sugerencias.Clear();
            foreach (var proyecto in proyectos)
            {
                var sugerenciasProyecto = await ApiService.GetSugerenciasByProyectoAsync(proyecto.Idproyecto);
                foreach (var s in sugerenciasProyecto)
                    _sugerencias.Add(s);
            }

            // Stats
            LblStat1Value.Text = _proyectos.Count.ToString();
            LblStat1Title.Text = "Proyectos";
            LblStat2Value.Text = _salas.Count.ToString();
            LblStat2Title.Text = "Salas";
            LblStat3Value.Text = _mediciones.Count.ToString();
            LblStat3Title.Text = "Mediciones";

            // Texto del empty de sugerencias según rol
            LblEmptySugerencias.Text = esArquitecto
                ? "Tus clientes aún no han enviado sugerencias"
                : "Aún no has enviado sugerencias";
        }
        catch (Exception ex)
        {
            await ShowToastAsync($"Error al cargar: {ex.Message}");
        }
    }

    private void OnTab1Tapped(object? sender, TappedEventArgs e) => SetTab(1);
    private void OnTab2Tapped(object? sender, TappedEventArgs e) => SetTab(2);
    private void OnTab3Tapped(object? sender, TappedEventArgs e) => SetTab(3);

    private void SetTab(int tab)
    {
        SuggestionsListCollectionView.IsVisible = tab == 1;
        MeasurementsListCollectionView.IsVisible = tab == 2;
        RoomsListCollectionView.IsVisible = tab == 3;

        Tab1Icon.Source = tab == 1 ? "ic_suggestions_active.svg" : "ic_suggestions_inactive.svg";
        Tab2Icon.Source = tab == 2 ? "ic_measure_active.svg" : "ic_measure_inactive.svg";
        Tab3Icon.Source = tab == 3 ? "ic_rooms_active.svg" : "ic_rooms_inactive.svg";

        Tab1Underline.IsVisible = tab == 1;
        Tab2Underline.IsVisible = tab == 2;
        Tab3Underline.IsVisible = tab == 3;
    }

    // ====================== COPIAR CÓDIGO DE SALA ======================
    private async void OnCopyRoomCodeTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ProyectoDto proyecto) return;
        if (string.IsNullOrWhiteSpace(proyecto.Codigosalaactiva))
        {
            await ShowToastAsync("Esta sala no tiene código activo");
            return;
        }

        await Clipboard.Default.SetTextAsync(proyecto.Codigosalaactiva);
        await ShowToastAsync($"Código copiado: {proyecto.Codigosalaactiva}");
    }

    // ====================== MENÚ ======================
    private async void OnOpenMenuSheetClicked(object? sender, TappedEventArgs e)
    {
        BuildMenuItems();
        MenuBackdrop.IsVisible = true;
        await MenuBackdrop.FadeToAsync(1, 200);
        MenuSheetModal.IsVisible = true;
        await Task.WhenAll(
            MenuSheetModal.FadeToAsync(1, 250),
            MenuSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut)
        );
    }

    private async void OnCloseMenuSheetClicked(object? sender, EventArgs e) => await CloseMenuSheetAsync();
    private async void OnCloseMenuSheetTapped(object? sender, TappedEventArgs e) => await CloseMenuSheetAsync();

    private async Task CloseMenuSheetAsync()
    {
        await Task.WhenAll(
            MenuSheetModal.FadeToAsync(0, 200),
            MenuSheetModal.TranslateToAsync(0, 600, 250, Easing.CubicIn)
        );
        MenuSheetModal.IsVisible = false;
        await MenuBackdrop.FadeToAsync(0, 200);
        MenuBackdrop.IsVisible = false;
    }

    private void BuildMenuItems()
    {
        MenuItemsStack.Children.Clear();
        var items = new List<MenuItemModel>();

        items.Add(new MenuItemModel
        {
            Icon = "ic_edit.svg",
            Text = "Editar perfil",
            Action = () => { OnEditProfileClicked(null, EventArgs.Empty); return Task.CompletedTask; }
        });

        if (UserSession.Rol == "Arquitecto")
        {
            items.Add(new MenuItemModel
            {
                Icon = "ic_ruler.svg",
                Text = "Mediciones",
                Action = () => Shell.Current.GoToAsync("//MeasureARPage")
            });

            items.Add(new MenuItemModel
            {
                Icon = "ic_users.svg",
                Text = "Gestionar usuarios",
                Action = () => Navigation.PushModalAsync(new AdminUsersPage())
            });
        }
        else
        {
            items.Add(new MenuItemModel
            {
                Icon = "ic_ruler.svg",
                Text = "Mis mediciones",
                Action = () => Shell.Current.GoToAsync("//MeasureARPage")
            });
        }

        items.Add(new MenuItemModel
        {
            Icon = "ic_logout.svg",
            Text = "Cerrar sesión",
            Action = async () =>
            {
                bool confirm = await AlertService.ShowAlertAsync("Cerrar Sesión", "¿Estás seguro?", "Sí, Salir", "Cancelar");
                if (confirm) Application.Current!.Windows[0].Page = new LoginPage();
            }
        });

        foreach (var item in items)
        {
            bool isLogout = item.Text == "Cerrar sesión";
            MenuItemsStack.Children.Add(BuildMenuItem(item.Icon, item.Text, item.Action, isLogout));
        }
    }

    private View BuildMenuItem(string icon, string text, Func<Task> action, bool isDanger = false)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            Padding = new Thickness(20, 14),
            ColumnSpacing = 16
        };

        row.Children.Add(new Image { Source = icon, WidthRequest = 22, HeightRequest = 22, VerticalOptions = LayoutOptions.Center });

        var label = new Label
        {
            Text = text,
            FontSize = 15,
            VerticalOptions = LayoutOptions.Center,
            TextColor = isDanger ? Color.FromArgb("#FF3B30") : Color.FromArgb("#000000")
        };
        Grid.SetColumn(label, 1);
        row.Children.Add(label);

        var border = new Border { BackgroundColor = Colors.Transparent, StrokeThickness = 0, Content = row };
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (s, ev) => { await CloseMenuSheetAsync(); await action(); };
        border.GestureRecognizers.Add(tap);
        return border;
    }

    // ====================== CREAR ======================
    private async void OnOpenCreateSheetClicked(object? sender, TappedEventArgs e)
    {
        BuildCreateItems();
        CreateBackdrop.IsVisible = true;
        await CreateBackdrop.FadeToAsync(1, 200);
        CreateSheetModal.IsVisible = true;
        await Task.WhenAll(
            CreateSheetModal.FadeToAsync(1, 250),
            CreateSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut)
        );
    }

    private async void OnCloseCreateSheetClicked(object? sender, EventArgs e) => await CloseCreateSheetAsync();
    private async void OnCloseCreateSheetTapped(object? sender, TappedEventArgs e) => await CloseCreateSheetAsync();

    private async Task CloseCreateSheetAsync()
    {
        await Task.WhenAll(
            CreateSheetModal.FadeToAsync(0, 200),
            CreateSheetModal.TranslateToAsync(0, 600, 250, Easing.CubicIn)
        );
        CreateSheetModal.IsVisible = false;
        await CreateBackdrop.FadeToAsync(0, 200);
        CreateBackdrop.IsVisible = false;
    }

    private void BuildCreateItems()
    {
        CreateItemsStack.Children.Clear();
        var items = new List<MenuItemModel>();

        if (UserSession.Rol == "Arquitecto")
        {
            items.Add(new MenuItemModel { Icon = "ic_layers.svg", Text = "Nuevo proyecto", Action = () => Shell.Current.GoToAsync("//DashboardPage") });
            items.Add(new MenuItemModel { Icon = "ic_layers.svg", Text = "Nueva sala", Action = () => Shell.Current.GoToAsync("//DashboardPage") });
        }
        else
        {
            items.Add(new MenuItemModel { Icon = "ic_link.svg", Text = "Unirse con código", Action = () => Shell.Current.GoToAsync("//DashboardPage") });
        }

        items.Add(new MenuItemModel { Icon = "ic_ruler.svg", Text = "Medir con AR", Action = () => Shell.Current.GoToAsync("//MeasureARPage") });
        items.Add(new MenuItemModel { Icon = "ic_palette.svg", Text = "Diseño 3D", Action = () => Shell.Current.GoToAsync("//DesignPage") });
        items.Add(new MenuItemModel
        {
            Icon = "ic_edit.svg",
            Text = "Editar perfil",
            Action = async () => { await CloseCreateSheetAsync(); OnEditProfileClicked(null, EventArgs.Empty); }
        });

        foreach (var item in items)
            CreateItemsStack.Children.Add(BuildSheetItem(item.Icon, item.Text, item.Action));
    }

    private View BuildSheetItem(string icon, string text, Func<Task> action)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star }
            },
            Padding = new Thickness(20, 14),
            ColumnSpacing = 16
        };

        row.Children.Add(new Image { Source = icon, WidthRequest = 22, HeightRequest = 22, VerticalOptions = LayoutOptions.Center });

        var label = new Label { Text = text, FontSize = 15, VerticalOptions = LayoutOptions.Center, TextColor = Color.FromArgb("#000000") };
        Grid.SetColumn(label, 1);
        row.Children.Add(label);

        var border = new Border { BackgroundColor = Colors.Transparent, StrokeThickness = 0, Content = row };
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (s, ev) => { await CloseCreateSheetAsync(); await action(); };
        border.GestureRecognizers.Add(tap);
        return border;
    }

    // ====================== EDITAR PERFIL ======================
    private async void OnEditProfileClicked(object? sender, EventArgs e)
    {
        EditNombreEntry.Text = UserSession.Nombre;
        EditApellidoEntry.Text = UserSession.Apellido;
        EditTelefonoEntry.Text = UserSession.Telefono;
        EditDireccionEntry.Text = UserSession.Direccion;
        EditDocumentoEntry.Text = UserSession.Numerodocumento;

        EditProfileBackdrop.IsVisible = true;
        await EditProfileBackdrop.FadeToAsync(1, 200);
        EditProfileSheetModal.IsVisible = true;
        await Task.WhenAll(
            EditProfileSheetModal.FadeToAsync(1, 250),
            EditProfileSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut)
        );
    }

    private async void OnCloseEditProfileSheetClicked(object? sender, EventArgs e) => await CloseEditProfileSheetAsync();
    private async void OnCloseEditProfileSheetTapped(object? sender, TappedEventArgs e) => await CloseEditProfileSheetAsync();

    private async Task CloseEditProfileSheetAsync()
    {
        await Task.WhenAll(
            EditProfileSheetModal.FadeToAsync(0, 200),
            EditProfileSheetModal.TranslateToAsync(0, 800, 250, Easing.CubicIn)
        );
        EditProfileSheetModal.IsVisible = false;
        await EditProfileBackdrop.FadeToAsync(0, 200);
        EditProfileBackdrop.IsVisible = false;
    }

    private async void OnSubmitEditProfileClicked(object? sender, EventArgs e) => await SubmitEditProfileAsync();
    private async void OnSubmitEditProfileTapped(object? sender, TappedEventArgs e) => await SubmitEditProfileAsync();

    private async Task SubmitEditProfileAsync()
    {
        var req = new ActualizarUsuarioRequest
        {
            Idusuario = UserSession.Idusuario,
            Nombre = EditNombreEntry.Text ?? "",
            Apellido = EditApellidoEntry.Text ?? "",
            Email = UserSession.Email ?? "",
            Contrasena = "dummy_password",
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
            await CloseEditProfileSheetAsync();
            await ShowToastAsync("Perfil actualizado");
        }
        else
        {
            await AlertService.ShowAlertAsync("Error", res.Message, "OK");
        }
    }

    // ====================== SUBIR AVATAR ======================
    private async void OnUploadProfilePictureClicked(object? sender, TappedEventArgs e)
    {
        try
        {
            var photos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { Title = "Selecciona una foto de perfil" });
            var photo = System.Linq.Enumerable.FirstOrDefault(photos);

            if (photo != null)
            {
                AvatarInitialsLabel.IsVisible = false;
                ProfileImage.IsVisible = true;

                using var streamForUi = await photo.OpenReadAsync();
                var memoryStreamUi = new MemoryStream();
                await streamForUi.CopyToAsync(memoryStreamUi);
                memoryStreamUi.Position = 0;
                ProfileImage.Source = ImageSource.FromStream(() => memoryStreamUi);

                using var memoryStream = new MemoryStream();
                using var stream = await photo.OpenReadAsync();
                await stream.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

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
                    await ShowToastAsync("Foto actualizada");
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    await AlertService.ShowAlertAsync("Error", $"No se pudo guardar: {error}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            await AlertService.ShowAlertAsync("Error", $"No se pudo subir la foto: {ex.Message}", "OK");
        }
    }

    private async void OnShareProfileClicked(object? sender, EventArgs e)
    {
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Compartir perfil",
            Text = $"Mira mi perfil de ArchiSpace: {UserSession.Nombre} {UserSession.Apellido} ({UserSession.Email})"
        });
    }

    private async void OnManageUsersClicked(object? sender, TappedEventArgs e)
    {
        if (UserSession.Rol != "Arquitecto")
        {
            await ShowToastAsync("Solo arquitectos pueden gestionar usuarios");
            return;
        }
        await Navigation.PushModalAsync(new AdminUsersPage());
    }

    // ====================== TOAST ======================
    private async Task ShowToastAsync(string message)
    {
        AppleToastMessage.Text = message;
        AppleToast.IsVisible = true;
        await AppleToast.FadeToAsync(1, 300);
        await Task.Delay(2500);
        await AppleToast.FadeToAsync(0, 300);
        AppleToast.IsVisible = false;
    }

    // ====================== MODELO ======================
    private class MenuItemModel
    {
        public string Icon { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public Func<Task> Action { get; set; } = () => Task.CompletedTask;
    }
}