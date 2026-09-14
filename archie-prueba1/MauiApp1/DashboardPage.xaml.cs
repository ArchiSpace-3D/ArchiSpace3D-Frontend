using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1;

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
        await CargarProyectosAsync();
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
        var list = await ApiService.GetNotificacionesByUsuarioAsync(UserSession.Idusuario);
        NotificationCountBadge.Text = $"{list?.Count ?? 0} nuevas";

        if (list != null)
        {
            foreach(var n in list.Take(3))
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
                    Margin = new Thickness(0,0,0,10),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) },
                    Content = row
                };
                card.Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.05f, Offset = new Point(0,2), Radius = 5 };
                NotificationsContainer.Children.Add(card);
            }
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.9, 50); await btn.ScaleToAsync(1.0, 50); }
        await CargarProyectosAsync();
        await CargarNotificacionesDashAsync();
    }

    private async void OnNotificationsClicked(object sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new NotificationsPage());
    }

    private async void OnSelectProjectClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ProyectoDto p)
        {
            UserSession.ActiveProject = p;
            ActualizarProyectoActivoUI();
            await ShowToastAsync($"Seleccionaste: {p.Nombre}");
        }
    }

    private async void OnActiveProjectDetailsClicked(object sender, EventArgs e)
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
        SheetProjectCode.Text = $"Presupuesto: ";
        
        DetailsBackdrop.IsVisible = true;
        await DetailsBackdrop.FadeToAsync(1, 200);
        ProjectDetailsSheetModal.IsVisible = true;
        await ProjectDetailsSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut);
    }

    private async void OnCloseDetailsSheetClicked(object sender, EventArgs e)
    {
        await CloseDetailsSheet();
    }

    private async Task CloseDetailsSheet()
    {
        await ProjectDetailsSheetModal.TranslateToAsync(0, 600, 250, Easing.CubicIn);
        await DetailsBackdrop.FadeToAsync(0, 200);
        DetailsBackdrop.IsVisible = false;
        ProjectDetailsSheetModal.IsVisible = false;
    }

    private async void OnSetAsActiveProjectClicked(object sender, EventArgs e)
    {
        await CloseDetailsSheet();
    }

    private async void OnEnterDesignClicked(object sender, EventArgs e)
    {
        await CloseDetailsSheet();
        await Shell.Current.GoToAsync("//DesignPage");
    }

    private async void OnEditarProyectoFromDetailsClicked(object sender, EventArgs e)
    {
        await CloseDetailsSheet();
        if (UserSession.ActiveProject == null) return;
        
        EditNombreProyecto.Text = UserSession.ActiveProject.Nombre;
        EditDescripcionProyecto.Text = UserSession.ActiveProject.Descripcion;
        EditUbicacionProyecto.Text = UserSession.ActiveProject.Ubicacion;
        EditPresupuestoProyecto.Text = UserSession.ActiveProject.Presupuesto.ToString();

        EditProjectBackdrop.IsVisible = true;
        await EditProjectBackdrop.FadeToAsync(1, 200);
        EditProjectSheetModal.IsVisible = true;
        await EditProjectSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut);
    }

    private async void OnCloseEditProjectSheetClicked(object sender, EventArgs e)
    {
        await CloseEditProjectSheet();
    }

    private async Task CloseEditProjectSheet()
    {
        await EditProjectSheetModal.TranslateToAsync(0, 600, 250, Easing.CubicIn);
        await EditProjectBackdrop.FadeToAsync(0, 200);
        EditProjectBackdrop.IsVisible = false;
        EditProjectSheetModal.IsVisible = false;
    }

    private async void OnSubmitEditarProyectoClicked(object sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;
        var p = UserSession.ActiveProject;
        
        var req = new ActualizarProyectoRequest
        {
            Nombre = EditNombreProyecto.Text ?? p.Nombre,
            Descripcion = EditDescripcionProyecto.Text ?? p.Descripcion,
            Ubicacion = EditUbicacionProyecto.Text ?? p.Ubicacion,
            Presupuesto = decimal.TryParse(EditPresupuestoProyecto.Text, out var v) ? v : p.Presupuesto,
            Estado = p.Estado
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

    private async void OnEliminarProyectoFromDetailsClicked(object sender, EventArgs e)
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
    }

    private async void OnOpenNewProjectSheetClicked(object sender, EventArgs e)
    {
        NewProjectBackdrop.IsVisible = true;
        await NewProjectBackdrop.FadeToAsync(1, 200);
        await NewProjectSheetCard.TranslateToAsync(0, 0, 300, Easing.CubicOut);
    }

    private async void OnCloseNewProjectSheetClicked(object sender, EventArgs e)
    {
        await CloseNewProjectSheet();
    }

    private async Task CloseNewProjectSheet()
    {
        await NewProjectSheetCard.TranslateToAsync(0, 600, 250, Easing.CubicIn);
        await NewProjectBackdrop.FadeToAsync(0, 200);
        NewProjectBackdrop.IsVisible = false;
    }

    private async void OnSubmitCrearProyectoClicked(object sender, EventArgs e)
    {
        var p = new CrearProyectoRequest
        {
            Idarquitecto = UserSession.Rol == "Arquitecto" ? UserSession.Idusuario : 0,
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
        }
    }

    private async void OnOpenJoinCodeSheetClicked(object sender, EventArgs e)
    {
        JoinCodeBackdrop.IsVisible = true;
        await JoinCodeBackdrop.FadeToAsync(1, 200);
        JoinCodeSheetModal.IsVisible = true;
        await JoinCodeSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut);
    }

    private async void OnCloseJoinCodeSheetClicked(object sender, EventArgs e)
    {
        await CloseJoinCodeSheet();
    }

    private async Task CloseJoinCodeSheet()
    {
        await JoinCodeSheetModal.TranslateToAsync(0, 600, 250, Easing.CubicIn);
        await JoinCodeBackdrop.FadeToAsync(0, 200);
        JoinCodeBackdrop.IsVisible = false;
        JoinCodeSheetModal.IsVisible = false;
    }

    private async void OnSubmitJoinCodeClicked(object sender, EventArgs e)
    {
        var codigo = JoinCodeEntry.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(codigo)) return;
        
        var (success, msg) = await ApiService.UsarInvitacionAsync(codigo);
        if (success)
        {
            await CloseJoinCodeSheet();
            await CargarProyectosAsync();
            await ShowToastAsync("Proyecto vinculado con éxito");
        }
        else
        {
            await ShowAlertAsync("Error", msg, "OK");
        }
    }

    private async void OnGenerarInvitacionClicked(object sender, EventArgs e)
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

    private async void OnVerMedicionesClicked(object sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;

        // Close details sheet first
        await CloseDetailsSheet();

        // Show measurements sheet
        MeasurementsBackdrop.IsVisible = true;
        await MeasurementsBackdrop.FadeToAsync(1, 200);
        MeasurementsSheetModal.IsVisible = true;
        await MeasurementsSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut);

        // Load data
        LoadingMeasurementsIndicator.IsVisible = true;
        LoadingMeasurementsIndicator.IsRunning = true;
        MeasurementsCollectionView.ItemsSource = null;

        var mediciones = await ApiService.GetMedicionesByProyectoAsync(UserSession.ActiveProject.Idproyecto);
        
        LoadingMeasurementsIndicator.IsRunning = false;
        LoadingMeasurementsIndicator.IsVisible = false;
        
        MeasurementsCollectionView.ItemsSource = mediciones;
    }

    private async void OnCloseMeasurementsSheetClicked(object sender, EventArgs e)
    {
        await MeasurementsSheetModal.TranslateToAsync(0, 600, 250, Easing.CubicIn);
        await MeasurementsBackdrop.FadeToAsync(0, 200);
        MeasurementsBackdrop.IsVisible = false;
        MeasurementsSheetModal.IsVisible = false;
    }

    private async void OnReglaARClicked(object sender, EventArgs e)
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
        return Application.Current!.Windows[0].Page!.DisplayAlertAsync(title, message, cancel);
    }

    private Task<bool> ShowAlertConfirmAsync(string title, string message, string accept, string cancel)
    {
        return Application.Current!.Windows[0].Page!.DisplayAlertAsync(title, message, accept, cancel);
    }
}

