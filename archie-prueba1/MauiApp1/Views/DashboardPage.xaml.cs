using Plugin.LocalNotification;
using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace MauiApp1.Views;

public partial class DashboardPage : ContentPage
{
    private readonly ObservableCollection<ProyectoDto> _proyectos = new();
    private FileResult? _imagenProyectoSeleccionada;

    public DashboardPage()
    {
        InitializeComponent();
        ProjectsFeedCollectionView.ItemsSource = _proyectos;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        bool esArquitecto = UserSession.Rol == "Arquitecto";
        NewProjectActionContainer.IsVisible = esArquitecto;
        NewRoomActionContainer.IsVisible = esArquitecto;
        JoinRoomActionContainer.IsVisible = !esArquitecto;
        BtnGenerarInvitacion.IsVisible = esArquitecto;
        BtnSheetEditarProyecto.IsVisible = esArquitecto;
        BtnSheetEliminarProyecto.IsVisible = esArquitecto;

        await CargarProyectosAsync();
        LoadRooms();

        await Task.WhenAll(
            MainScroll.FadeToAsync(1, 600, Easing.CubicOut),
            MainScroll.TranslateToAsync(0, 0, 600, Easing.CubicOut)
        );

        await CargarNotificacionesDashAsync();
    }

    // ====================== SALAS (HISTORIAS) ======================
    private void LoadRooms()
    {
        if (UserSession.ActiveProject == null && _proyectos.Count > 0)
            UserSession.ActiveProject = _proyectos.FirstOrDefault();

        RoomsCollectionView.ItemsSource = _proyectos.ToList();
    }

    private async void OnRoomTappedFromCollection(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ProyectoDto proyecto) return;

        UserSession.ActiveProject = proyecto;
        LoadRooms();
        await ShowToastAsync($"Sala: {proyecto.Nombre}");
    }

    // ====================== FEED ======================
    private async void OnProjectNameTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ProyectoDto proyecto) return;

        UserSession.ActiveProject = proyecto;
        LoadRooms();
        await Shell.Current.GoToAsync("//DesignPage");
    }

    private async void OnProjectImageTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ProyectoDto proyecto) return;

        UserSession.ActiveProject = proyecto;
        LoadRooms();
        await Shell.Current.GoToAsync("//DesignPage");
    }

    private async void OnProjectMenuTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ProyectoDto proyecto) return;

        UserSession.ActiveProject = proyecto;
        LoadRooms();
        OnActiveProjectDetailsClicked(null, EventArgs.Empty);
    }

    // ====================== PROYECTOS ======================
    private async Task CargarProyectosAsync()
    {
        _proyectos.Clear();
        try
        {
            List<ProyectoDto> list = UserSession.Rol == "Arquitecto"
                ? await ApiService.GetProyectosByArquitectoAsync(UserSession.Idusuario)
                : await ApiService.GetProyectosByClienteAsync(UserSession.Idusuario);

            foreach (var prj in list) _proyectos.Add(prj);
        }
        catch { }
    }

    private async Task CargarNotificacionesDashAsync()
    {
        NotificationsContainer.Children.Clear();
        var list = await ApiService.GetNotificacionesAsync();

        int unreadCount = list?.Count(n => n.Leida != true) ?? 0;
        NotificationCountBadge.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();
        NotificationCountBadge.IsVisible = unreadCount > 0;

        if (list != null)
        {
            foreach (var n in list.Take(3))
            {
                var textStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
                textStack.Children.Add(new Label { Text = n.Mensaje, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#001D39") });
                textStack.Children.Add(new Label { Text = n.FechaFormateada, FontSize = 11, TextColor = Color.FromArgb("#6EA2B3") });

                var row = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = GridLength.Star } },
                    Padding = new Thickness(15)
                };
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

    private async void OnNotificationsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new NotificationsPage());
    }

    // ====================== DETALLE PROYECTO ======================
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
        await Task.WhenAll(
            ProjectDetailsSheetModal.FadeToAsync(1, 250),
            ProjectDetailsSheetModal.ScaleToAsync(1, 250, Easing.SpringOut)
        );

        var idCliente = UserSession.ActiveProject.Idcliente;
        if (idCliente.HasValue && idCliente.Value > 0)
        {
            var cliente = await ApiService.GetUsuarioByIdAsync(idCliente.Value);
            SheetProjectClient.Text = cliente != null ? $"Cliente: {cliente.Nombre}" : "Cliente: Desconocido";
        }
        else
        {
            SheetProjectClient.Text = "Cliente: Sin asignar";
        }
    }

    private async void OnCloseDetailsSheetClicked(object? sender, EventArgs e) => await CloseDetailsSheet();

    private async Task CloseDetailsSheet()
    {
        await Task.WhenAll(
            ProjectDetailsSheetModal.FadeToAsync(0, 200),
            ProjectDetailsSheetModal.ScaleToAsync(0.8, 200, Easing.CubicIn)
        );
        ProjectDetailsSheetModal.IsVisible = false;
        await DetailsBackdrop.FadeToAsync(0, 200);
        DetailsBackdrop.IsVisible = false;
    }

    private async void OnEnterDesignClicked(object? sender, EventArgs e)
    {
        await CloseDetailsSheet();
        await Shell.Current.GoToAsync("//DesignPage");
    }

    // ====================== EDITAR PROYECTO ======================
    private async void OnEditarProyectoFromDetailsClicked(object? sender, EventArgs e)
    {
        await CloseDetailsSheet();
        if (UserSession.ActiveProject == null) return;

        EditNombreProyecto.Text = UserSession.ActiveProject.Nombre;
        EditDescripcionProyecto.Text = UserSession.ActiveProject.Descripcion;
        EditUbicacionProyecto.Text = UserSession.ActiveProject.Ubicacion;
        EditPresupuestoProyecto.Text = UserSession.ActiveProject.Presupuesto.ToString();

        if (EditEstadoProyecto.Items.Contains(UserSession.ActiveProject.Estado))
            EditEstadoProyecto.SelectedItem = UserSession.ActiveProject.Estado;
        else
            EditEstadoProyecto.SelectedItem = "Borrador";

        EditProjectBackdrop.IsVisible = true;
        await EditProjectBackdrop.FadeToAsync(1, 200);
        EditProjectSheetModal.IsVisible = true;
        await Task.WhenAll(
            EditProjectSheetModal.FadeToAsync(1, 250),
            EditProjectSheetModal.ScaleToAsync(1, 250, Easing.SpringOut)
        );
    }

    private async void OnCloseEditProjectSheetClicked(object? sender, TappedEventArgs e) => await CloseEditProjectSheet();

    private async Task CloseEditProjectSheet()
    {
        await Task.WhenAll(
            EditProjectSheetModal.FadeToAsync(0, 200),
            EditProjectSheetModal.ScaleToAsync(0.8, 200, Easing.CubicIn)
        );
        EditProjectSheetModal.IsVisible = false;
        await EditProjectBackdrop.FadeToAsync(0, 200);
        EditProjectBackdrop.IsVisible = false;
    }

    private async void OnSubmitEditarProyectoClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;
        var p = UserSession.ActiveProject;
        var req = new ActualizarProyectoRequest
        {
            Idproyecto = p.Idproyecto,
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
            p.Estado = req.Estado;

            await CloseEditProjectSheet();
            await CargarProyectosAsync();
            LoadRooms();
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
            await CloseDetailsSheet();
            await CargarProyectosAsync();
            LoadRooms();
        }
        else
        {
            ShowCustomAlert("Error al Eliminar", res.Message, true);
        }
    }

    // ====================== NUEVO PROYECTO ======================
    private async void OnOpenNewProjectSheetClicked(object? sender, EventArgs e)
    {
        _imagenProyectoSeleccionada = null;
        ImgPreviewProyecto.Source = null;
        ImgPreviewProyecto.IsVisible = false;
        ImgPlaceholderProyecto.IsVisible = true;
        LblNombreImagenProyecto.IsVisible = false;

        await CargarClientesParaPickerAsync();

        NewProjectBackdrop.IsVisible = true;
        await NewProjectBackdrop.FadeToAsync(1, 200);
        NewProjectSheetCard.IsVisible = true;
        await Task.WhenAll(
            NewProjectSheetCard.FadeToAsync(1, 250),
            NewProjectSheetCard.TranslateToAsync(0, 0, 300, Easing.CubicOut)
        );
    }

    private async Task CargarClientesParaPickerAsync()
    {
        try
        {
            var usuarios = await ApiService.GetUsuariosAsync();
            var clientes = usuarios?.Where(u => u.Rol == "Cliente").ToList() ?? new List<UsuarioDto>();

            PickerClienteProyecto.ItemsSource = clientes;

            if (clientes.Count == 0)
            {
                LblSinClientes.IsVisible = true;
                PickerClienteProyecto.IsEnabled = false;
            }
            else
            {
                LblSinClientes.IsVisible = false;
                PickerClienteProyecto.IsEnabled = true;
            }
        }
        catch
        {
            PickerClienteProyecto.ItemsSource = new List<UsuarioDto>();
            LblSinClientes.IsVisible = true;
            PickerClienteProyecto.IsEnabled = false;
        }
    }

    private async void OnSeleccionarImagenProyectoClicked(object? sender, TappedEventArgs e)
    {
        try
        {
            var resultado = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona una imagen para el proyecto",
                FileTypes = FilePickerFileType.Images
            });

            if (resultado == null) return;

            var extensionesValidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(resultado.FileName).ToLowerInvariant();
            if (!extensionesValidas.Contains(ext))
            {
                await ShowAlertAsync("Formato no válido", "Solo se permiten imágenes JPG, PNG o WEBP.", "OK");
                return;
            }

            _imagenProyectoSeleccionada = resultado;

            var stream = await resultado.OpenReadAsync();
            ImgPreviewProyecto.Source = ImageSource.FromStream(() => stream);
            ImgPreviewProyecto.IsVisible = true;
            ImgPlaceholderProyecto.IsVisible = false;
            LblNombreImagenProyecto.Text = resultado.FileName;
            LblNombreImagenProyecto.IsVisible = true;
        }
        catch (Exception ex)
        {
            await ShowAlertAsync("Error", $"No se pudo seleccionar la imagen: {ex.Message}", "OK");
        }
    }

    private async void OnCloseNewProjectSheetClicked(object? sender, EventArgs e) => await CloseNewProjectSheet();

    private async Task CloseNewProjectSheet()
    {
        EntryNombreProyecto.Text = "";
        EntryDescripcionProyecto.Text = "";
        EntryUbicacionProyecto.Text = "";
        EntryPresupuestoProyecto.Text = "";
        PickerEstadoProyecto.SelectedItem = null;
        PickerClienteProyecto.SelectedItem = null;

        _imagenProyectoSeleccionada = null;
        ImgPreviewProyecto.Source = null;
        ImgPreviewProyecto.IsVisible = false;
        ImgPlaceholderProyecto.IsVisible = true;
        LblNombreImagenProyecto.IsVisible = false;

        await Task.WhenAll(
            NewProjectSheetCard.FadeToAsync(0, 200),
            NewProjectSheetCard.TranslateToAsync(0, 400, 250, Easing.CubicIn)
        );
        NewProjectSheetCard.IsVisible = false;
        await NewProjectBackdrop.FadeToAsync(0, 200);
        NewProjectBackdrop.IsVisible = false;
    }

    private async void OnSubmitCrearProyectoClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EntryNombreProyecto.Text))
        {
            ShowCustomAlert("Falta el nombre", "El nombre del proyecto es obligatorio.", true);
            return;
        }

        decimal presupuesto = 0;
        if (!string.IsNullOrWhiteSpace(EntryPresupuestoProyecto.Text))
            decimal.TryParse(EntryPresupuestoProyecto.Text, out presupuesto);

        int? idCliente = null;
        if (PickerClienteProyecto.SelectedItem is UsuarioDto cliente)
            idCliente = cliente.Idusuario;

        string estado = PickerEstadoProyecto.SelectedItem?.ToString() ?? "Borrador";

        var p = new CrearProyectoRequest
        {
            Idarquitecto = UserSession.Idusuario,
            Idcliente = idCliente,
            Nombre = EntryNombreProyecto.Text.Trim(),
            Ubicacion = EntryUbicacionProyecto.Text?.Trim() ?? "",
            Estado = estado,
            Descripcion = string.IsNullOrWhiteSpace(EntryDescripcionProyecto.Text)
                ? "Nuevo Proyecto"
                : EntryDescripcionProyecto.Text.Trim(),
            Presupuesto = presupuesto
        };

        var btn = sender as Button;
        if (btn != null) btn.IsEnabled = false;

        try
        {
            var res = await ApiService.CrearProyectoAsync(p);

            if (!res.Success || res.Data == null)
            {
                ShowCustomAlert("Error", res.Message, true);
                return;
            }

            if (_imagenProyectoSeleccionada != null)
            {
                var (okImg, msgImg, urlImg) = await ApiService.SubirImagenProyectoAsync(res.Data.Idproyecto, _imagenProyectoSeleccionada);

                if (!okImg)
                {
                    await ShowAlertAsync("Proyecto creado",
                        $"El proyecto se creó, pero la imagen no se pudo subir:\n{msgImg}", "OK");
                }
            }

            await CloseNewProjectSheet();
            await CargarProyectosAsync();
            LoadRooms();
            ShowCustomAlert("¡Proyecto Creado!", $"El proyecto '{p.Nombre}' ha sido guardado exitosamente.", false);
        }
        finally
        {
            if (btn != null) btn.IsEnabled = true;
        }
    }

    // ====================== NUEVA SALA ======================
    private async void OnOpenNewRoomSheetClicked(object? sender, EventArgs e)
    {
        await CargarProyectosSinSalaAsync();

        NewRoomBackdrop.IsVisible = true;
        await NewRoomBackdrop.FadeToAsync(1, 200);
        NewRoomSheetModal.IsVisible = true;
        await Task.WhenAll(
            NewRoomSheetModal.FadeToAsync(1, 250),
            NewRoomSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut)
        );
    }

    private async Task CargarProyectosSinSalaAsync()
    {
        await CargarProyectosAsync();

        var sinSala = _proyectos
            .Where(p => string.IsNullOrWhiteSpace(p.Codigosalaactiva))
            .ToList();

        PickerProyectoSinSala.ItemsSource = sinSala;
        PickerProyectoSinSala.SelectedItem = null;
        NewRoomNombreEntry.Text = "";

        if (sinSala.Count == 0)
        {
            LblSinProyectos.IsVisible = true;
            PickerProyectoSinSala.IsEnabled = false;
        }
        else
        {
            LblSinProyectos.IsVisible = false;
            PickerProyectoSinSala.IsEnabled = true;
        }
    }

    private void OnProyectoSinSalaSelected(object? sender, EventArgs e)
    {
        if (PickerProyectoSinSala.SelectedItem is ProyectoDto proyecto)
        {
            if (string.IsNullOrWhiteSpace(NewRoomNombreEntry.Text))
                NewRoomNombreEntry.Text = proyecto.Nombre;
        }
    }

    private async void OnCloseNewRoomSheetClicked(object? sender, TappedEventArgs e) => await CloseNewRoomSheet();

    private async Task CloseNewRoomSheet()
    {
        await Task.WhenAll(
            NewRoomSheetModal.FadeToAsync(0, 200),
            NewRoomSheetModal.TranslateToAsync(0, 400, 250, Easing.CubicIn)
        );
        NewRoomSheetModal.IsVisible = false;
        await NewRoomBackdrop.FadeToAsync(0, 200);
        NewRoomBackdrop.IsVisible = false;
    }

    private async void OnSubmitNewRoomClicked(object? sender, EventArgs e)
    {
        if (PickerProyectoSinSala.SelectedItem is not ProyectoDto proyectoSeleccionado)
        {
            await ShowAlertAsync("Aviso", "Selecciona un proyecto para activar su sala.", "OK");
            return;
        }

        var codigo = $"ARQ-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
        var (okInv, msgInv, data) = await ApiService.CrearInvitacionAsync(proyectoSeleccionado.Idproyecto, codigo);

        if (okInv && data != null)
        {
            await CloseNewRoomSheet();
            NewRoomNombreEntry.Text = "";
            PickerProyectoSinSala.SelectedItem = null;

            await CargarProyectosAsync();
            LoadRooms();

            await ShowAlertAsync("Sala Creada",
                $"Código de invitación:\n\n{data.Codigo}\n\nCompártelo con tu cliente.",
                "Copiar");
            await Clipboard.Default.SetTextAsync(data.Codigo);
        }
        else
        {
            await ShowAlertAsync("Error", $"No se pudo generar el código: {msgInv}", "OK");
        }
    }

    // ====================== UNIRSE CON CÓDIGO ======================
    private async void OnOpenJoinCodeSheetClicked(object? sender, EventArgs e)
    {
        JoinCodeBackdrop.IsVisible = true;
        await JoinCodeBackdrop.FadeToAsync(1, 200);
        JoinCodeSheetModal.IsVisible = true;
        await Task.WhenAll(
            JoinCodeSheetModal.FadeToAsync(1, 250),
            JoinCodeSheetModal.ScaleToAsync(1, 250, Easing.SpringOut)
        );
    }

    private async void OnCloseJoinCodeSheetClicked(object? sender, TappedEventArgs e) => await CloseJoinCodeSheet();

    private async Task CloseJoinCodeSheet()
    {
        await Task.WhenAll(
            JoinCodeSheetModal.FadeToAsync(0, 200),
            JoinCodeSheetModal.ScaleToAsync(0.8, 200, Easing.CubicIn)
        );
        JoinCodeSheetModal.IsVisible = false;
        await JoinCodeBackdrop.FadeToAsync(0, 200);
        JoinCodeBackdrop.IsVisible = false;
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
            LoadRooms();
            await ShowToastAsync("Proyecto vinculado con éxito");
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
            await ShowAlertAsync("Código Generado",
                $"Comparte este código con tu cliente:\n\n{data.Codigo}",
                "Copiar");
            await Clipboard.Default.SetTextAsync(data.Codigo);
        }
        else
        {
            await ShowAlertAsync("Error", $"No se pudo generar la invitación: {msg}", "OK");
        }
    }

    // ====================== ESPACIOS / SUGERENCIAS ======================
    private async void OnVerEspaciosClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;
        await CloseDetailsSheet();
        await Navigation.PushModalAsync(new SpacesPage(UserSession.ActiveProject.Idproyecto));
    }

    private async void OnVerSugerenciasClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;
        await CloseDetailsSheet();
        await Navigation.PushModalAsync(new SuggestionsPage(UserSession.ActiveProject.Idproyecto));
    }

    // ====================== MEDICIONES ======================
    private async void OnVerMedicionesClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;

        await CloseDetailsSheet();

        MeasurementsBackdrop.IsVisible = true;
        await MeasurementsBackdrop.FadeToAsync(1, 200);
        MeasurementsSheetModal.IsVisible = true;
        await Task.WhenAll(
            MeasurementsSheetModal.FadeToAsync(1, 250),
            MeasurementsSheetModal.ScaleToAsync(1, 250, Easing.SpringOut)
        );

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
        await Task.WhenAll(
            MeasurementsSheetModal.FadeToAsync(0, 200),
            MeasurementsSheetModal.ScaleToAsync(0.8, 200, Easing.CubicIn)
        );
        MeasurementsSheetModal.IsVisible = false;
        await MeasurementsBackdrop.FadeToAsync(0, 200);
        MeasurementsBackdrop.IsVisible = false;
    }

    // ====================== HELPERS ======================
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
        => AlertService.ShowAlertAsync(title, message, cancel);

    private Task<bool> ShowAlertConfirmAsync(string title, string message, string accept, string cancel)
        => AlertService.ShowAlertAsync(title, message, accept, cancel);

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

    private async void OnCloseAlertClicked(object? sender, EventArgs e) => await CloseAlert();
    private async void OnCloseAlertClicked(object? sender, TappedEventArgs e) => await CloseAlert();

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