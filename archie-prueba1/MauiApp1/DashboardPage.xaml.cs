using System.Collections.ObjectModel;
using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1;

public partial class DashboardPage : ContentPage
{
    private readonly ObservableCollection<ProyectoDto> _proyectos = new ObservableCollection<ProyectoDto>();
    private ProyectoDto? _selectedProjectForSheet;

    public DashboardPage()
    {
        InitializeComponent();
        ProjectsCollectionView.ItemsSource = _proyectos;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        UpdateUserInfo();

        ActiveProjectCard.Opacity = 0;
        ActiveProjectCard.Scale = 0.95;
        ProjectsSection.Opacity = 0;
        ProjectsSection.TranslationY = 25;
        NotificationsSection.Opacity = 0;
        NotificationsSection.TranslationY = 25;

        _ = LoadProjectsAsync();
        _ = LoadNotificationsAsync();

        _ = ActiveProjectCard.FadeToAsync(1, 400, Easing.CubicOut);
        _ = ActiveProjectCard.ScaleToAsync(1.0, 450, Easing.CubicOut);
        await Task.Delay(80);
        _ = ProjectsSection.FadeToAsync(1, 400, Easing.CubicOut);
        _ = ProjectsSection.TranslateToAsync(0, 0, 450, Easing.CubicOut);
        await Task.Delay(80);
        _ = NotificationsSection.FadeToAsync(1, 400, Easing.CubicOut);
        await NotificationsSection.TranslateToAsync(0, 0, 450, Easing.CubicOut);
    }

    private void UpdateUserInfo()
    {
        string nombre = string.IsNullOrWhiteSpace(UserSession.Nombre) ? "Arquitecto" : UserSession.Nombre;
        WelcomeLabel.Text = $"¡Hola, {nombre}!";
        SubWelcomeLabel.Text = UserSession.IsAuthenticated
            ? $"Rol: {UserSession.Rol} • {UserSession.Email}"
            : "Modo sin conexión (Invitado)";

        string inicial = !string.IsNullOrEmpty(nombre) ? nombre.Substring(0, 1).ToUpper() : "A";
        HeaderAvatarInitials.Text = inicial;

        bool esArquitecto = UserSession.Rol == "Arquitecto";
        BtnNuevoProyecto.IsVisible = esArquitecto;
        BtnUnirseCodigo.IsVisible = !esArquitecto;
    }

    private async Task LoadProjectsAsync()
    {
        LoadingProjectsIndicator.IsRunning = true;
        LoadingProjectsIndicator.IsVisible = true;

        try
        {
            _proyectos.Clear();

            if (UserSession.IsAuthenticated)
            {
                var apiProjects = await ApiService.GetProyectosAsync();

                if (apiProjects != null && apiProjects.Count > 0)
                {
                    foreach (var proj in apiProjects)
                    {
                        _proyectos.Add(proj);
                    }

                    if (UserSession.ActiveProject == null || !_proyectos.Any(p => p.Idproyecto == UserSession.ActiveProject.Idproyecto))
                    {
                        UserSession.ActiveProject = apiProjects[0];
                    }

                    UpdateActiveProjectBanner();
                    return;
                }
            }

            EmptyProjectsLabel.Text = UserSession.IsAuthenticated
                ? "No hay proyectos registrados. ¡Crea el primero con '+ Nuevo'!"
                : "Modo offline. Inicia sesión para sincronizar tus proyectos.";

            if (!UserSession.IsAuthenticated)
            {
                var demo1 = new ProyectoDto
                {
                    Idproyecto = 1,
                    Nombre = "Edificio Nova (Demo)",
                    Estado = "En progreso",
                    Ubicacion = "Cra 7 #45-10",
                    Presupuesto = 150000000,
                    Fechaactualizacion = DateTime.Now.AddDays(-1)
                };
                _proyectos.Add(demo1);
                UserSession.ActiveProject = demo1;
                UpdateActiveProjectBanner();
            }
        }
        catch (Exception ex)
        {
            await ShowToastAsync($"Error al cargar proyectos: {ex.Message}");
        }
        finally
        {
            LoadingProjectsIndicator.IsRunning = false;
            LoadingProjectsIndicator.IsVisible = false;
        }
    }

    private void UpdateActiveProjectBanner()
    {
        if (UserSession.ActiveProject != null)
        {
            ActiveProjectBannerLabel.Text = UserSession.ActiveProject.Nombre;
            ActiveProjectStatusLabel.Text = UserSession.ActiveProject.EstadoNormalizado;
            ActiveProjectLocationLabel.Text = string.IsNullOrWhiteSpace(UserSession.ActiveProject.Ubicacion)
                ? "Ubicación no especificada"
                : UserSession.ActiveProject.Ubicacion;
        }
        else
        {
            ActiveProjectBannerLabel.Text = "Sin proyecto activo";
            ActiveProjectStatusLabel.Text = "N/A";
            ActiveProjectLocationLabel.Text = "Selecciona un proyecto abajo";
        }
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        _ = RefreshBorder.RotateToAsync(360, 400).ContinueWith(_ => RefreshBorder.Rotation = 0);
        await LoadProjectsAsync();
        await LoadNotificationsAsync();
        await ShowToastAsync("Proyectos y notificaciones actualizados");
    }

    private async void OnProfileClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//ProfilePage");
    }

    private async void OnEnterARClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
        await Navigation.PushModalAsync(new ARPage());
    }

    private void OnActiveProjectDetailsClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject != null)
        {
            OpenDetailsSheet(UserSession.ActiveProject);
        }
        else
        {
            _ = ShowToastAsync("No hay un proyecto activo seleccionado.");
        }
    }

    // ==================== SELECCION DE PROYECTO & BOTTOM SHEET DETALLES ====================

    private void OnProjectSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not ProyectoDto selected) return;
        ProjectsCollectionView.SelectedItem = null;
        OpenDetailsSheet(selected);
    }

    private async void OpenDetailsSheet(ProyectoDto project)
    {
        _selectedProjectForSheet = project;

        SheetProjectTitleLabel.Text = project.Nombre;
        SheetProjectStatusLabel.Text = project.EstadoNormalizado;
        SheetProjectLocationLabel.Text = string.IsNullOrWhiteSpace(project.Ubicacion) ? "No especificada" : project.Ubicacion;
        SheetProjectBudgetLabel.Text = project.PresupuestoFormateado;
        SheetProjectDateLabel.Text = project.FechaFormateada;
        SheetProjectCodeLabel.Text = string.IsNullOrWhiteSpace(project.Codigosalaactiva) ? "No asignada" : project.Codigosalaactiva;

        bool esArquitecto = UserSession.Rol == "Arquitecto";
        BtnSheetGenerarInvitacion.IsVisible = esArquitecto;
        BtnSheetEliminarProyecto.IsVisible = esArquitecto;

        ProjectDetailsSheetModal.IsVisible = true;
        DetailsBackdrop.Opacity = 0;
        DetailsSheetCard.TranslationY = 500;

        _ = DetailsBackdrop.FadeToAsync(1.0, 250);
        await DetailsSheetCard.TranslateToAsync(0, 0, 300, Easing.CubicOut);
    }

    private async void OnCloseDetailsSheetClicked(object? sender, EventArgs e)
    {
        _ = DetailsBackdrop.FadeToAsync(0, 200);
        await DetailsSheetCard.TranslateToAsync(0, 500, 250, Easing.CubicIn);
        ProjectDetailsSheetModal.IsVisible = false;
    }

    private async void OnSetProjectAsActiveClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
        if (_selectedProjectForSheet != null)
        {
            UserSession.ActiveProject = _selectedProjectForSheet;
            UpdateActiveProjectBanner();
            OnCloseDetailsSheetClicked(null, EventArgs.Empty);
            await ShowToastAsync($"'{_selectedProjectForSheet.Nombre}' es ahora el proyecto activo.");
        }
    }

    private async void OnOpenArFromDetailsClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
        if (_selectedProjectForSheet != null)
        {
            UserSession.ActiveProject = _selectedProjectForSheet;
            UpdateActiveProjectBanner();
        }
        OnCloseDetailsSheetClicked(null, EventArgs.Empty);
        await Navigation.PushModalAsync(new ARPage());
    }

    private async void OnGenerarInvitacionFromDetailsClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
        if (_selectedProjectForSheet == null) return;

        string codigo = $"ARCHI-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
        var (success, message, invitacion) = await ApiService.CrearInvitacionAsync(_selectedProjectForSheet.Idproyecto, codigo);

        if (success && invitacion != null)
        {
            await Clipboard.Default.SetTextAsync(invitacion.Codigo);
            OnCloseDetailsSheetClicked(null, EventArgs.Empty);
            await ShowToastAsync($"Código copiado al portapapeles: {invitacion.Codigo}");
        }
        else
        {
            await ShowToastAsync(message);
        }
    }

    private async void OnEliminarProyectoFromDetailsClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
        if (_selectedProjectForSheet == null) return;

        int idProj = _selectedProjectForSheet.Idproyecto;
        var (success, message) = await ApiService.EliminarProyectoAsync(idProj);

        OnCloseDetailsSheetClicked(null, EventArgs.Empty);

        if (success)
        {
            if (UserSession.ActiveProject?.Idproyecto == idProj)
            {
                UserSession.ActiveProject = null;
            }
            await LoadProjectsAsync();
            await ShowToastAsync("Proyecto eliminado.");
        }
        else
        {
            await ShowToastAsync(message);
        }
    }

    // ==================== BOTTOM SHEET: NUEVO PROYECTO ====================

    private async void OnOpenNewProjectSheetClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.94, 60); await btn.ScaleToAsync(1.0, 60); }
        if (!UserSession.IsAuthenticated)
        {
            await ShowToastAsync("Inicia sesión para crear proyectos.");
            return;
        }

        EntryNombreProyecto.Text = "";
        EntryDescripcionProyecto.Text = "";
        EntryUbicacionProyecto.Text = "";
        EntryPresupuestoProyecto.Text = "";

        NewProjectSheetModal.IsVisible = true;
        NewProjectBackdrop.Opacity = 0;
        NewProjectSheetCard.TranslationY = 450;

        _ = NewProjectBackdrop.FadeToAsync(1.0, 250);
        await NewProjectSheetCard.TranslateToAsync(0, 0, 300, Easing.CubicOut);
    }

    private async void OnCloseNewProjectSheetClicked(object? sender, EventArgs e)
    {
        _ = NewProjectBackdrop.FadeToAsync(0, 200);
        await NewProjectSheetCard.TranslateToAsync(0, 450, 250, Easing.CubicIn);
        NewProjectSheetModal.IsVisible = false;
    }

    private async void OnSubmitCrearProyectoClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }

        string nombre = EntryNombreProyecto.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(nombre))
        {
            await ShowToastAsync("Ingresa un nombre para el proyecto.");
            return;
        }

        decimal.TryParse(EntryPresupuestoProyecto.Text, out decimal presupuesto);
        string codigoSala = $"SALA-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";

        var nuevo = new CrearProyectoRequest
        {
            Idarquitecto = UserSession.Idusuario,
            Idcliente = UserSession.Idusuario,
            Nombre = nombre,
            Descripcion = EntryDescripcionProyecto.Text?.Trim(),
            Ubicacion = EntryUbicacionProyecto.Text?.Trim(),
            Presupuesto = presupuesto > 0 ? presupuesto : null,
            Estado = "En progreso",
            Codigosalaactiva = codigoSala
        };

        var (success, message, creado) = await ApiService.CrearProyectoAsync(nuevo);
        OnCloseNewProjectSheetClicked(null, EventArgs.Empty);

        if (success && creado != null)
        {
            UserSession.ActiveProject = creado;
            await LoadProjectsAsync();
            await ShowToastAsync($"¡Proyecto '{creado.Nombre}' creado!");
        }
        else
        {
            await ShowToastAsync(message);
        }
    }

    // ==================== BOTTOM SHEET: UNIRSE CON CODIGO ====================

    private async void OnOpenJoinCodeSheetClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.94, 60); await btn.ScaleToAsync(1.0, 60); }
        if (!UserSession.IsAuthenticated)
        {
            await ShowToastAsync("Inicia sesión para unirte a proyectos.");
            return;
        }

        EntryCodigoInvitacion.Text = "";
        JoinCodeSheetModal.IsVisible = true;
        JoinCodeBackdrop.Opacity = 0;
        JoinCodeSheetCard.TranslationY = 350;

        _ = JoinCodeBackdrop.FadeToAsync(1.0, 250);
        await JoinCodeSheetCard.TranslateToAsync(0, 0, 300, Easing.CubicOut);
    }

    private async void OnCloseJoinCodeSheetClicked(object? sender, EventArgs e)
    {
        _ = JoinCodeBackdrop.FadeToAsync(0, 200);
        await JoinCodeSheetCard.TranslateToAsync(0, 350, 250, Easing.CubicIn);
        JoinCodeSheetModal.IsVisible = false;
    }

    private async void OnSubmitUsarCodigoClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }

        string codigo = EntryCodigoInvitacion.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(codigo))
        {
            await ShowToastAsync("Ingresa un código de invitación.");
            return;
        }

        var (success, message) = await ApiService.UsarInvitacionAsync(codigo);
        OnCloseJoinCodeSheetClicked(null, EventArgs.Empty);

        if (success)
        {
            await LoadProjectsAsync();
            await ShowToastAsync("¡Te has vinculado al proyecto exitosamente!");
        }
        else
        {
            await ShowToastAsync(message);
        }
    }

    // ==================== NOTIFICACIONES ====================

    private async Task LoadNotificationsAsync()
    {
        NotificationsContainer.Children.Clear();

        if (!UserSession.IsAuthenticated)
        {
            NotificationCountBadge.Text = "";
            return;
        }

        try
        {
            var notifs = await ApiService.GetNotificacionesAsync();

            if (notifs != null && notifs.Count > 0)
            {
                int sinLeer = notifs.Count(n => n.Leida != true);
                NotificationCountBadge.Text = sinLeer > 0 ? $"{sinLeer} sin leer" : "Al día";

                foreach (var n in notifs.Take(5))
                {
                    var card = new Border
                    {
                        BackgroundColor = Color.FromArgb("#FFFFFF"),
                        StrokeThickness = 0,
                        Padding = new Thickness(16, 12),
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(18) }
                    };

                    var grid = new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitionCollection
                        {
                            new ColumnDefinition { Width = GridLength.Auto },
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto }
                        },
                        ColumnSpacing = 12
                    };

                    var icon = new Image
                    {
                        Source = "ic_bell.svg",
                        WidthRequest = 20,
                        HeightRequest = 20,
                        VerticalOptions = LayoutOptions.Center
                    };
                    Grid.SetColumn(icon, 0);

                    var textStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
                    textStack.Children.Add(new Label { Text = n.Mensaje, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2C3639") });
                    textStack.Children.Add(new Label { Text = n.FechaFormateada, FontSize = 11, TextColor = Color.FromArgb("#7A8485") });
                    Grid.SetColumn(textStack, 1);

                    if (n.Leida != true)
                    {
                        var checkBtn = new Border
                        {
                            BackgroundColor = Color.FromArgb("#A27B5B"),
                            WidthRequest = 32,
                            HeightRequest = 32,
                            StrokeThickness = 0,
                            VerticalOptions = LayoutOptions.Center,
                            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) }
                        };
                        var checkImg = new Image { Source = "ic_check.svg", WidthRequest = 14, HeightRequest = 14, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
                        checkBtn.Content = checkImg;

                        int idNotif = n.Idnotificacion;
                        var tap = new TapGestureRecognizer();
                        tap.Tapped += async (s, e) =>
                        {
                            await ApiService.MarcarNotificacionLeidaAsync(idNotif);
                            await LoadNotificationsAsync();
                            await ShowToastAsync("Notificación marcada como leída");
                        };
                        checkBtn.GestureRecognizers.Add(tap);
                        Grid.SetColumn(checkBtn, 2);
                        grid.Children.Add(checkBtn);
                    }

                    grid.Children.Add(icon);
                    grid.Children.Add(textStack);
                    card.Content = grid;
                    NotificationsContainer.Children.Add(card);
                }
            }
            else
            {
                NotificationCountBadge.Text = "0 nuevas";
                NotificationsContainer.Children.Add(new Label
                {
                    Text = "No tienes notificaciones pendientes.",
                    FontSize = 13,
                    TextColor = Color.FromArgb("#7A8485"),
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 4)
                });
            }
        }
        catch { }
    }

    // ==================== APPLE TOAST ====================

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
