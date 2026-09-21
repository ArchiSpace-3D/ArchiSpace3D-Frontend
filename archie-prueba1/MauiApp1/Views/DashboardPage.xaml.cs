using Plugin.LocalNotification;
using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1.Views;

public partial class DashboardPage : ContentPage
{
    private readonly ObservableCollection<ProyectoDto> _proyectos = new();

    public DashboardPage()
    {
        InitializeComponent();
        ProjectsCollectionView.ItemsSource = _proyectos;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        NombreUsuarioLabel.Text = string.IsNullOrWhiteSpace(UserSession.Nombre) ? "Usuario" : UserSession.Nombre;

        bool esArquitecto = UserSession.Rol == "Arquitecto";
        NewProjectActionContainer.IsVisible = esArquitecto;
        BtnGenerarInvitacion.IsVisible = esArquitecto;
        BtnSheetEditarProyecto.IsVisible = esArquitecto;
        BtnSheetEliminarProyecto.IsVisible = esArquitecto;

        await CargarProyectosAsync();

        await Task.WhenAll(
            MainScroll.FadeToAsync(1, 600, Easing.CubicOut),
            MainScroll.TranslateToAsync(0, 0, 600, Easing.CubicOut)
        );

        await CargarNotificacionesDashAsync();
        ActualizarProyectoActivoUI();
    }

    private void ActualizarProyectoActivoUI()
    {
        var p = UserSession.ActiveProject;
        if (p == null)
        {
            LblNombreProyectoActivo.Text = "Selecciona un proyecto";
            LblEstadoProyectoActivo.Text = "Ninguno";
            LblUbicacionProyectoActivo.Text = "--";
        }
        else
        {
            LblNombreProyectoActivo.Text = p.Nombre;
            LblEstadoProyectoActivo.Text = p.Estado;
            LblUbicacionProyectoActivo.Text = p.Ubicacion;
        }
    }

    private async Task CargarProyectosAsync()
    {
        LoadingProjectsIndicator.IsRunning = true;
        LoadingProjectsIndicator.IsVisible = true;
        _proyectos.Clear();

        try
        {
            List<ProyectoDto> list = UserSession.Rol == "Arquitecto" ? 
                await ApiService.GetProyectosByArquitectoAsync(UserSession.Idusuario) :
                await ApiService.GetProyectosByClienteAsync(UserSession.Idusuario);
            
            foreach (var prj in list) _proyectos.Add(prj);
        }
        catch { }
        finally
        {
            LoadingProjectsIndicator.IsRunning = false;
            LoadingProjectsIndicator.IsVisible = false;
        }
    }

    private async Task CargarNotificacionesDashAsync()
    {
        NotificationsContainer.Children.Clear();
        var list = await ApiService.GetNotificacionesAsync();
        NotificationCountBadge.Text = $"{list?.Count ?? 0} nuevas";

        if (list != null)
        {
            foreach (var n in list.Take(3))
            {
                var textStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
                textStack.Children.Add(new Label { Text = n.Mensaje, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#001D39") });
                textStack.Children.Add(new Label { Text = n.FechaFormateada, FontSize = 11, TextColor = Color.FromArgb("#6EA2B3") });

                var row = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = GridLength.Star } }, Padding = new Thickness(15) };
                row.Children.Add(textStack);

                var card = new Border
                {
                    BackgroundColor = Color.FromArgb("#FFFFFF"),
                    StrokeThickness = 0,
                    Margin = new Thickness(0, 0, 0, 10),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) },
                    Content = row
                };
                card.Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.05f, Offset = new Point(0, 2), Radius = 5 };
                NotificationsContainer.Children.Add(card);
            }
        }
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.9, 50); await btn.ScaleToAsync(1.0, 50); }
        await CargarProyectosAsync();
        await CargarNotificacionesDashAsync();
    }

    private async void OnToggleThemeClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) 
        { 
            _ = btn.ScaleToAsync(0.8, 150, Easing.CubicIn).ContinueWith(t => btn.ScaleToAsync(1.0, 150, Easing.CubicOut));
        }
        await this.FadeToAsync(0.1, 250, Easing.SinInOut);
        
        if (Application.Current!.UserAppTheme == AppTheme.Dark)
        {
            Application.Current.UserAppTheme = AppTheme.Light;
            
        }
        else
        {
            Application.Current.UserAppTheme = AppTheme.Dark;
            
        }
        this.RotationY = 0;
        this.Scale = 1;
        await this.FadeToAsync(1.0, 300, Easing.SinInOut);
    }

    private async void OnNotificationsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new NotificationsPage());
    }

    private async void OnSelectProjectClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ProyectoDto p)
        {
            UserSession.ActiveProject = p;
            ActualizarProyectoActivoUI();
            await ShowToastAsync($"Seleccionaste: {p.Nombre}");
        }
    }

    private async void OnActiveProjectDetailsClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null)
        {
            await ShowAlertAsync("Aviso", "Selecciona un proyecto primero.", "OK");
            return;
        }
        SheetProjectName.Text = UserSession.ActiveProject.Nombre;
        SheetProjectDesc.Text = UserSession.ActiveProject.Descripcion ?? "Sin descripción";
        SheetProjectUbicacion.Text = UserSession.ActiveProject.Ubicacion ?? "--";
        SheetProjectEstado.Text = UserSession.ActiveProject.Estado ?? "--";
        SheetProjectCode.Text = $"Presupuesto: {UserSession.ActiveProject.Presupuesto:C}";
        SheetProjectClient.Text = "Cliente: Cargando...";
        
        DetailsBackdrop.IsVisible = true;
        await DetailsBackdrop.FadeToAsync(1, 200);
        ProjectDetailsSheetModal.IsVisible = true;
        await Task.WhenAll(ProjectDetailsSheetModal.FadeToAsync(1, 250), ProjectDetailsSheetModal.ScaleToAsync(1, 250, Easing.SpringOut));

        if (UserSession.ActiveProject.Idcliente > 0)
        {
            var cliente = await ApiService.GetUsuarioByIdAsync(UserSession.ActiveProject.Idcliente);
            if (cliente != null)
            {
                SheetProjectClient.Text = $"Cliente: {cliente.Nombre} {cliente.Apellido}";
            }
            else
            {
                SheetProjectClient.Text = "Cliente: Desconocido";
            }
        }
        else
        {
            SheetProjectClient.Text = "Cliente: Sin asignar";
        }
    }

    private async void OnCloseDetailsSheetClicked(object? sender, EventArgs e)
    {
        await CloseDetailsSheet();
    }

    private async Task CloseDetailsSheet()
    {
        await Task.WhenAll(ProjectDetailsSheetModal.FadeToAsync(0, 200), ProjectDetailsSheetModal.ScaleToAsync(0.8, 200, Easing.CubicIn));
        ProjectDetailsSheetModal.IsVisible = false;
        await DetailsBackdrop.FadeToAsync(0, 200);
        DetailsBackdrop.IsVisible = false;
        ProjectDetailsSheetModal.IsVisible = false;
    }

    private async void OnSetAsActiveProjectClicked(object? sender, EventArgs e)
    {
        await CloseDetailsSheet();
    }

    private async void OnEnterDesignClicked(object? sender, EventArgs e)
    {
        await CloseDetailsSheet();
        await Shell.Current.GoToAsync("//DesignPage");
    }

    private async void OnEditarProyectoFromDetailsClicked(object? sender, EventArgs e)
    {
        await CloseDetailsSheet();
        if (UserSession.ActiveProject == null) return;
        EditNombreProyecto.Text = UserSession.ActiveProject.Nombre;
        EditDescripcionProyecto.Text = UserSession.ActiveProject.Descripcion;
        EditUbicacionProyecto.Text = UserSession.ActiveProject.Ubicacion;
        EditPresupuestoProyecto.Text = UserSession.ActiveProject.Presupuesto.ToString();
        
        if (EditEstadoProyecto.Items.Contains(UserSession.ActiveProject.Estado))
        {
            EditEstadoProyecto.SelectedItem = UserSession.ActiveProject.Estado;
        }
        else
        {
            EditEstadoProyecto.SelectedItem = "Borrador";
        }

        EditProjectBackdrop.IsVisible = true;
        await EditProjectBackdrop.FadeToAsync(1, 200);
        EditProjectSheetModal.IsVisible = true;
        EditProjectSheetModal.IsVisible = true;
        await Task.WhenAll(EditProjectSheetModal.FadeToAsync(1, 250), EditProjectSheetModal.ScaleToAsync(1, 250, Easing.SpringOut));
    }

    private async void OnCloseEditProjectSheetClicked(object? sender, TappedEventArgs e)
    {
        await CloseEditProjectSheet();
    }

    private async Task CloseEditProjectSheet()
    {
        await Task.WhenAll(EditProjectSheetModal.FadeToAsync(0, 200), EditProjectSheetModal.ScaleToAsync(0.8, 200, Easing.CubicIn));
        EditProjectSheetModal.IsVisible = false;
        await EditProjectBackdrop.FadeToAsync(0, 200);
        EditProjectBackdrop.IsVisible = false;
        EditProjectSheetModal.IsVisible = false;
    }

    private async void OnSubmitEditarProyectoClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;
        var p = UserSession.ActiveProject;
        var req = new ActualizarProyectoRequest
        {
            Nombre = EditNombreProyecto.Text ?? p.Nombre,
            Descripcion = EditDescripcionProyecto.Text ?? p.Descripcion,
            Ubicacion = EditUbicacionProyecto.Text ?? p.Ubicacion,
            Presupuesto = decimal.TryParse(EditPresupuestoProyecto.Text, out var v) ? v : p.Presupuesto,
            Estado = EditEstadoProyecto.SelectedItem?.ToString() ?? p.Estado
        };

        var res = await ApiService.ActualizarProyectoAsync(p.Idproyecto, req);
        if (res.Success)
        {
            p.Nombre = req.Nombre;
            p.Descripcion = req.Descripcion;
            p.Ubicacion = req.Ubicacion;
            p.Presupuesto = req.Presupuesto;
            
            await CloseEditProjectSheet();
            await CargarProyectosAsync();
            ActualizarProyectoActivoUI();
        }
    }

    private async void OnEliminarProyectoFromDetailsClicked(object? sender, EventArgs e)
    {
        var confirm = await ShowAlertConfirmAsync("Eliminar", "¿Borrar proyecto?", "Sí", "No");
        if (!confirm || UserSession.ActiveProject == null) return;
        
        var res = await ApiService.EliminarProyectoAsync(UserSession.ActiveProject.Idproyecto);
        if (res.Success)
        {
            UserSession.ActiveProject = null;
            ActualizarProyectoActivoUI();
            await CloseDetailsSheet();
            await CargarProyectosAsync();
        }
        else
        {
            ShowCustomAlert("Error al Eliminar", res.Message, true);
        }
    }

    private async void OnOpenNewProjectSheetClicked(object? sender, EventArgs e)
    {
        NewProjectBackdrop.IsVisible = true;
        await NewProjectBackdrop.FadeToAsync(1, 200);
        NewProjectSheetCard.IsVisible = true;
        await Task.WhenAll(NewProjectSheetCard.FadeToAsync(1, 250), NewProjectSheetCard.ScaleToAsync(1, 250, Easing.SpringOut));
    }

    private async void OnCloseNewProjectSheetClicked(object? sender, EventArgs e)
    {
        await CloseNewProjectSheet();
    }

    private async Task CloseNewProjectSheet()
    {
        await Task.WhenAll(NewProjectSheetCard.FadeToAsync(0, 200), NewProjectSheetCard.ScaleToAsync(0.8, 200, Easing.CubicIn));
        NewProjectSheetCard.IsVisible = false;
        await NewProjectBackdrop.FadeToAsync(0, 200);
        NewProjectBackdrop.IsVisible = false;
    }

    private async void OnSubmitCrearProyectoClicked(object? sender, EventArgs e)
    {
        var p = new CrearProyectoRequest
        {
            Idarquitecto = UserSession.Rol == "Arquitecto" ? UserSession.Idusuario : UserSession.Idusuario, Idcliente = UserSession.Idusuario,
            Nombre = EntryNombreProyecto.Text ?? "Nuevo",
            Ubicacion = EntryUbicacionProyecto.Text ?? "",
            Estado = "Borrador",
            Descripcion = "Nuevo Proyecto",
            Presupuesto = 0
        };
        var res = await ApiService.CrearProyectoAsync(p);
        if (res.Success)
        {
            await CloseNewProjectSheet();
            await CargarProyectosAsync();
            ShowCustomAlert("¡Proyecto Creado!", $"El proyecto '{p.Nombre}' ha sido guardado exitosamente.", false);
        }
        else
        {
            ShowCustomAlert("Error", res.Message, true);
        }
    }

    private async void OnOpenJoinCodeSheetClicked(object? sender, EventArgs e)
    {
        JoinCodeBackdrop.IsVisible = true;
        await JoinCodeBackdrop.FadeToAsync(1, 200);
        JoinCodeSheetModal.IsVisible = true;
        JoinCodeSheetModal.IsVisible = true;
        await Task.WhenAll(JoinCodeSheetModal.FadeToAsync(1, 250), JoinCodeSheetModal.ScaleToAsync(1, 250, Easing.SpringOut));
    }

    private async void OnCloseJoinCodeSheetClicked(object? sender, TappedEventArgs e)
    {
        await CloseJoinCodeSheet();
    }

    private async Task CloseJoinCodeSheet()
    {
        await Task.WhenAll(JoinCodeSheetModal.FadeToAsync(0, 200), JoinCodeSheetModal.ScaleToAsync(0.8, 200, Easing.CubicIn));
        JoinCodeSheetModal.IsVisible = false;
        await JoinCodeBackdrop.FadeToAsync(0, 200);
        JoinCodeBackdrop.IsVisible = false;
        JoinCodeSheetModal.IsVisible = false;
    }

    private async void OnSubmitJoinCodeClicked(object? sender, EventArgs e)
    {
        var codigo = JoinCodeEntry.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(codigo)) return;
        
        var (success, msg) = await ApiService.UsarInvitacionAsync(codigo);
        if (success)
        {
            await CloseJoinCodeSheet();
            await CargarProyectosAsync();
            await ShowToastAsync("Proyecto vinculado con éÉxito");
        }
        else
        {
            await ShowAlertAsync("Error", msg, "OK");
        }
    }

    private async void OnGenerarInvitacionClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;
        
        string codigoGenerado = $"ARQ-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
        var (success, msg, data) = await ApiService.CrearInvitacionAsync(UserSession.ActiveProject.Idproyecto, codigoGenerado);
        if (success && data != null)
        {
            await ShowAlertAsync("Código Generado", $"Comparte este código con tu cliente para que se una al proyecto:\n\n{data.Codigo}", "Copiar");
            await Clipboard.Default.SetTextAsync(data.Codigo);
        }
        else
        {
            await ShowAlertAsync("Error", $"No se pudo generar la invitación: {msg}", "OK");
        }
    }

    private async void OnVerMedicionesClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;

        await CloseDetailsSheet();

        MeasurementsBackdrop.IsVisible = true;
        await MeasurementsBackdrop.FadeToAsync(1, 200);
        MeasurementsSheetModal.IsVisible = true;
        MeasurementsSheetModal.IsVisible = true;
        await Task.WhenAll(MeasurementsSheetModal.FadeToAsync(1, 250), MeasurementsSheetModal.ScaleToAsync(1, 250, Easing.SpringOut));

        LoadingMeasurementsIndicator.IsVisible = true;
        LoadingMeasurementsIndicator.IsRunning = true;
        MeasurementsCollectionView.ItemsSource = null;

        var mediciones = await ApiService.GetMediciónesByProyectoAsync(UserSession.ActiveProject.Idproyecto);
        
        LoadingMeasurementsIndicator.IsRunning = false;
        LoadingMeasurementsIndicator.IsVisible = false;
        
        MeasurementsCollectionView.ItemsSource = mediciones;
    }

    private async void OnCloseMeasurementsSheetClicked(object? sender, TappedEventArgs e)
    {
        await Task.WhenAll(MeasurementsSheetModal.FadeToAsync(0, 200), MeasurementsSheetModal.ScaleToAsync(0.8, 200, Easing.CubicIn));
        MeasurementsSheetModal.IsVisible = false;
        await MeasurementsBackdrop.FadeToAsync(0, 200);
        MeasurementsBackdrop.IsVisible = false;
        MeasurementsSheetModal.IsVisible = false;
    }

    private async void OnReglaARClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    private async Task ShowToastAsync(string message)
    {
        AppleToastMessage.Text = message;
        AppleToast.IsVisible = true;
        await AppleToast.FadeToAsync(1, 300);
        await Task.Delay(3000);
        await AppleToast.FadeToAsync(0, 300);
        AppleToast.IsVisible = false;
    }

    private Task ShowAlertAsync(string title, string message, string cancel)
    {
        return AlertService.ShowAlertAsync(title, message, cancel);
    }

    private Task<bool> ShowAlertConfirmAsync(string title, string message, string accept, string cancel)
    {
        return AlertService.ShowAlertAsync(title, message, accept, cancel);
    }

    public void ShowCustomAlert(string title, string message, bool isError = false)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            AlertTitle.Text = title;
            AlertMessage.Text = message;
            
            if (isError)
            {
                AlertIconBox.BackgroundColor = Color.FromArgb("#FAD2E1");
                AlertIconLabel.Text = "✕";
                AlertIconLabel.TextColor = Color.FromArgb("#C9184A");
                AlertButton.BackgroundColor = Color.FromArgb("#FFB3C6");
                AlertButton.Text = "Cerrar";
            }
            else
            {
                AlertIconBox.BackgroundColor = Color.FromArgb("#D1F4E0");
                AlertIconLabel.Text = "✓";
                AlertIconLabel.TextColor = Color.FromArgb("#129740");
                AlertButton.BackgroundColor = Color.FromArgb("#A3E7C9");
                AlertButton.Text = "Continuar";
            }

            AlertBackdrop.IsVisible = true;
            AlertModal.IsVisible = true;
            await Task.WhenAll(
                AlertBackdrop.FadeToAsync(1, 200),
                AlertModal.FadeToAsync(1, 250),
                AlertModal.ScaleToAsync(1, 250, Easing.SpringOut)
            );
        });
    }

    private async void OnCloseAlertClicked(object? sender, EventArgs e) { await CloseAlert(); }
    private async void OnCloseAlertClicked(object? sender, TappedEventArgs e) { await CloseAlert(); }
    private async Task CloseAlert()
    {
        await Task.WhenAll(
            AlertBackdrop.FadeToAsync(0, 200),
            AlertModal.FadeToAsync(0, 200),
            AlertModal.ScaleToAsync(0.9, 200, Easing.CubicIn)
        );
        AlertBackdrop.IsVisible = false;
        AlertModal.IsVisible = false;
    }
}








