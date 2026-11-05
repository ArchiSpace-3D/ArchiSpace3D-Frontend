using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace MauiApp1.Views;

public partial class ProfilePage : ContentPage
{
    private readonly ObservableCollection<ProyectoDto> _proyectos = new();
    private readonly ObservableCollection<MediciónDto> _mediciones = new();
    private readonly ObservableCollection<ProyectoDto> _salas = new();
    private readonly ObservableCollection<SugerenciaDto> _sugerencias = new();
    private int _currentTab = 1;

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

        bool esArquitecto = UserSession.EsArquitecto;
        BtnManageUsers.IsVisible = esArquitecto;
        ProfileActionsGrid.ColumnDefinitions.Clear();
        ProfileActionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        ProfileActionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        if (esArquitecto)
            ProfileActionsGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        SetTab(_currentTab);
        CargarDatosUsuarioUI();
        await CargarDatosAsync();

        await Task.WhenAll(
            MainScroll.FadeToAsync(1, 500, Easing.CubicOut),
            MainScroll.TranslateToAsync(0, 0, 500, Easing.CubicOut)
        );
    }

    private void CargarDatosUsuarioUI()
    {
        string nombreCompleto = string.IsNullOrWhiteSpace(UserSession.Apellido)
            ? UserSession.Nombre
            : $"{UserSession.Nombre} {UserSession.Apellido}";

        NombreUsuarioLabel.Text = string.IsNullOrWhiteSpace(nombreCompleto) ? "Usuario" : nombreCompleto;
        EmailUsuarioLabel.Text = UserSession.Email;
        RolBadgeLabel.Text = UserSession.EsArquitecto ? "Arquitecto" : "Cliente";

        string inicial = !string.IsNullOrEmpty(UserSession.Nombre)
            ? UserSession.Nombre.Substring(0, 1).ToUpperInvariant()
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
            bool esArquitecto = UserSession.EsArquitecto;

            var proyectos = esArquitecto
                ? await ApiService.GetProyectosByArquitectoAsync(UserSession.Idusuario)
                : await ApiService.GetProyectosByClienteAsync(UserSession.Idusuario);

            _proyectos.Clear();
            foreach (var p in proyectos) _proyectos.Add(p);

            // Salas = proyectos con código de sala activa
            _salas.Clear();
            foreach (var p in proyectos.Where(x => !string.IsNullOrWhiteSpace(x.Codigosalaactiva)))
                _salas.Add(p);

            // Mediciones y Sugerencias de todos los proyectos del usuario
            _mediciones.Clear();
            _sugerencias.Clear();

            foreach (var proyecto in proyectos)
            {
                var medsTask = ApiService.GetMediciónesByProyectoAsync(proyecto.Idproyecto);
                var sugsTask = ApiService.GetSugerenciasByProyectoAsync(proyecto.Idproyecto);
                await Task.WhenAll(medsTask, sugsTask);

                if (medsTask.Result != null)
                {
                    foreach (var m in medsTask.Result)
                        _mediciones.Add(m);
                }

                if (sugsTask.Result != null)
                {
                    foreach (var s in sugsTask.Result)
                        _sugerencias.Add(s);
                }
            }

            // Stats
            LblStat1Value.Text = _proyectos.Count.ToString();
            LblStat1Title.Text = "Proyectos";
            LblStat2Value.Text = _salas.Count.ToString();
            LblStat2Title.Text = "Salas";
            LblStat3Value.Text = _mediciones.Count.ToString();
            LblStat3Title.Text = "Mediciones";

            LblEmptySugerencias.Text = esArquitecto
                ? "Tus clientes aún no han enviado sugerencias"
                : "Aún no has enviado sugerencias en tus proyectos";

            LblEmptySalas.Text = esArquitecto
                ? "Activa una sala desde Inicio para generar el código de cliente"
                : "Los proyectos con sala activa aparecerán aquí";
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
        _currentTab = tab;
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        SuggestionsListCollectionView.IsVisible = tab == 1;
        MeasurementsListCollectionView.IsVisible = tab == 2;
        RoomsListCollectionView.IsVisible = tab == 3;

        Tab1Icon.Source = tab == 1
            ? (isDark ? "ic_suggestions_active_white.svg" : "ic_suggestions_active.svg")
            : "ic_suggestions_inactive.svg";
        Tab2Icon.Source = tab == 2
            ? (isDark ? "ic_measure_active_white.svg" : "ic_measure_active.svg")
            : "ic_measure_inactive.svg";
        Tab3Icon.Source = tab == 3
            ? (isDark ? "ic_rooms_active_white.svg" : "ic_rooms_active.svg")
            : "ic_rooms_inactive.svg";

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
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var items = new List<MenuItemModel>();

        items.Add(new MenuItemModel
        {
            Icon = isDark ? "ic_edit_white.svg" : "ic_edit.svg",
            Text = "Editar perfil",
            Action = () => { OnEditProfileClicked(null, EventArgs.Empty); return Task.CompletedTask; }
        });

        items.Add(new MenuItemModel
        {
            Icon = isDark ? "ic_sun.svg" : "ic_moon_dark.svg",
            Text = isDark ? "Cambiar a modo claro" : "Cambiar a modo oscuro",
            Action = () =>
            {
                bool nuevoOscuro = !isDark;
                Preferences.Default.Set("dark_mode", nuevoOscuro);
                if (Application.Current != null)
                {
                    Application.Current.UserAppTheme = nuevoOscuro ? AppTheme.Dark : AppTheme.Light;
                }
                SetTab(_currentTab);
                return Task.CompletedTask;
            }
        });

        if (UserSession.EsArquitecto)
        {
            items.Add(new MenuItemModel
            {
                Icon = isDark ? "ic_ruler_white.svg" : "ic_ruler.svg",
                Text = "Mediciones AR",
                Action = () => Shell.Current.GoToAsync("//MeasureARPage")
            });

            items.Add(new MenuItemModel
            {
                Icon = "ic_users.svg",
                Text = "Gestionar clientes",
                Action = () => Navigation.PushModalAsync(new AdminUsersPage())
            });
        }
        else
        {
            items.Add(new MenuItemModel
            {
                Icon = isDark ? "ic_ruler_white.svg" : "ic_ruler.svg",
                Text = "Mediciones del proyecto",
                Action = () => Shell.Current.GoToAsync("//MeasureARPage")
            });
        }

        items.Add(new MenuItemModel
        {
            Icon = "ic_logout.svg",
            Text = "Cerrar sesión",
            Action = async () =>
            {
                bool confirm = await AlertService.ShowAlertAsync("Cerrar Sesión", "¿Deseas cerrar tu sesión actual?", "Salir", "Cancelar");
                if (confirm)
                {
                    await UserSession.ClearSession();
                    Application.Current!.Windows[0].Page = new LoginPage();
                }
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
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
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
            TextColor = isDanger
                ? Color.FromArgb("#FF3B30")
                : Color.FromArgb(isDark ? "#FFFFFF" : "#000000")
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
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var items = new List<MenuItemModel>();

        if (UserSession.EsArquitecto)
        {
            items.Add(new MenuItemModel
            {
                Icon = isDark ? "nav_layers_white.svg" : "nav_layers.svg",
                Text = "Proyectos y salas",
                Action = () => Shell.Current.GoToAsync("//DashboardPage")
            });
        }
        else
        {
            items.Add(new MenuItemModel
            {
                Icon = isDark ? "ic_link_white.svg" : "ic_link.svg",
                Text = "Unirse con código",
                Action = () => Shell.Current.GoToAsync("//DashboardPage")
            });
        }

        items.Add(new MenuItemModel
        {
            Icon = isDark ? "ic_ruler_white.svg" : "ic_ruler.svg",
            Text = "Medir con AR",
            Action = () => Shell.Current.GoToAsync("//MeasureARPage")
        });
        items.Add(new MenuItemModel
        {
            Icon = "ic_palette.svg",
            Text = "Plano CAD 2D / 3D",
            Action = () => Shell.Current.GoToAsync("//DesignPage")
        });
        items.Add(new MenuItemModel
        {
            Icon = isDark ? "ic_edit_white.svg" : "ic_edit.svg",
            Text = "Editar perfil",
            Action = () => { OnEditProfileClicked(null, EventArgs.Empty); return Task.CompletedTask; }
        });

        foreach (var item in items)
            CreateItemsStack.Children.Add(BuildSheetItem(item.Icon, item.Text, item.Action));
    }

    private View BuildSheetItem(string icon, string text, Func<Task> action)
    {
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
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
            TextColor = Color.FromArgb(isDark ? "#FFFFFF" : "#000000")
        };
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
        EditNuevaContrasenaEntry.Text = string.Empty;

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
        string nombre = EditNombreEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(nombre))
        {
            await AlertService.ShowAlertAsync("Nombre requerido", "El nombre no puede estar vacío.", "Entendido");
            return;
        }

        string? nuevaPass = string.IsNullOrWhiteSpace(EditNuevaContrasenaEntry.Text)
            ? null
            : EditNuevaContrasenaEntry.Text.Trim();

        if (nuevaPass != null && nuevaPass.Length < 6)
        {
            await AlertService.ShowAlertAsync("Contraseña corta", "La nueva contraseña debe tener al menos 6 caracteres.", "Entendido");
            return;
        }

        var req = new ActualizarUsuarioRequest
        {
            Idusuario = UserSession.Idusuario,
            Nombre = nombre,
            Apellido = EditApellidoEntry.Text?.Trim() ?? string.Empty,
            Email = UserSession.Email ?? string.Empty,
            Contrasena = nuevaPass,
            Rol = UserSession.Rol ?? "Cliente",
            Telefono = string.IsNullOrWhiteSpace(EditTelefonoEntry.Text) ? null : EditTelefonoEntry.Text.Trim(),
            Direccion = string.IsNullOrWhiteSpace(EditDireccionEntry.Text) ? null : EditDireccionEntry.Text.Trim(),
            Tipodocumento = UserSession.Tipodocumento,
            Numerodocumento = string.IsNullOrWhiteSpace(EditDocumentoEntry.Text) ? null : EditDocumentoEntry.Text.Trim(),
            Avatarurl = UserSession.Avatarurl
        };

        var res = await ApiService.ActualizarUsuarioAsync(UserSession.Idusuario, req);
        if (res.Success)
        {
            UserSession.Nombre = req.Nombre;
            UserSession.Apellido = req.Apellido;
            UserSession.Telefono = req.Telefono;
            UserSession.Direccion = req.Direccion;
            UserSession.Numerodocumento = req.Numerodocumento;
            await UserSession.PersistCurrentFieldsAsync();

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
            var photo = photos?.FirstOrDefault();
            if (photo == null) return;

            AvatarInitialsLabel.IsVisible = false;
            ProfileImage.IsVisible = true;

            using (var streamForUi = await photo.OpenReadAsync())
            {
                var memoryStreamUi = new MemoryStream();
                await streamForUi.CopyToAsync(memoryStreamUi);
                memoryStreamUi.Position = 0;
                ProfileImage.Source = ImageSource.FromStream(() => memoryStreamUi);
            }

            var (ok, msg) = await ApiService.SubirAvatarUsuarioAsync(UserSession.Idusuario, photo);
            if (ok)
            {
                CargarDatosUsuarioUI();
                await ShowToastAsync("Foto de perfil actualizada");
            }
            else
            {
                await AlertService.ShowAlertAsync("Error", $"No se pudo guardar la foto: {msg}", "OK");
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
            Text = $"Perfil ArchiSpace: {UserSession.Nombre} {UserSession.Apellido} ({UserSession.Email})"
        });
    }

    private async void OnManageUsersClicked(object? sender, TappedEventArgs e)
    {
        if (!UserSession.EsArquitecto) return;
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

    private class MenuItemModel
    {
        public string Icon { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public Func<Task> Action { get; set; } = () => Task.CompletedTask;
    }
}
