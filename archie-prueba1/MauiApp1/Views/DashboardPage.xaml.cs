using System.Collections.ObjectModel;
using System.Globalization;
using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1.Views;

public partial class DashboardPage : ContentPage
{
    private readonly ObservableCollection<ProyectoDto> _proyectos = new();
    private FileResult? _imagenProyectoSeleccionada;
    private FileResult? _imagenEditProyectoSeleccionada;
    private List<UsuarioDto> _clientesCache = new();
    private bool _subscribedEvents;

    public DashboardPage()
    {
        InitializeComponent();
        ProjectsFeedCollectionView.ItemsSource = _proyectos;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_subscribedEvents)
        {
            SignalRService.NotificacionRecibida += OnNotificacionTiempoReal;
            if (Application.Current != null)
                Application.Current.RequestedThemeChanged += OnAppThemeChanged;
            _subscribedEvents = true;
        }

        bool esArquitecto = UserSession.EsArquitecto;
        NewProjectActionContainer.IsVisible = esArquitecto;
        JoinRoomActionContainer.IsVisible = !esArquitecto;
        RowInvitarCliente.IsVisible = esArquitecto;
        SepInvitarCliente.IsVisible = esArquitecto;
        SectionAdmin.IsVisible = esArquitecto;
        EmptyFeedSubLabel.Text = esArquitecto
            ? "Crea tu primer proyecto desde el botón superior izquierdo"
            : "Vincula el código de tu arquitecto desde el icono superior izquierdo";

        await CargarProyectosAsync();
        LoadRooms();

        await Task.WhenAll(
            MainScroll.FadeToAsync(1, 500, Easing.CubicOut),
            MainScroll.TranslateToAsync(0, 0, 500, Easing.CubicOut)
        );

        _ = CargarResumenesActividadAsync();
        _ = ConectarSalasSignalRAsync();
        await CargarNotificacionesDashAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_subscribedEvents)
        {
            SignalRService.NotificacionRecibida -= OnNotificacionTiempoReal;
            if (Application.Current != null)
                Application.Current.RequestedThemeChanged -= OnAppThemeChanged;
            _subscribedEvents = false;
        }
    }

    private void OnAppThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        LoadRooms();
        _ = CargarNotificacionesDashAsync();
    }

    private async void OnNotificacionTiempoReal(NotificacionDto notif)
    {
        await CargarNotificacionesDashAsync();
        if (!string.IsNullOrWhiteSpace(notif.Mensaje))
            await ShowToastAsync(notif.Mensaje);
    }

    private async Task ConectarSalasSignalRAsync()
    {
        try
        {
            SignalRService.RegistrarProyectos(_proyectos.Select(p => p.Idproyecto));
            if (!SignalRService.IsConnected)
                await SignalRService.ConnectAsync();
            await SignalRService.UnirseASalasDelUsuarioAsync();
        }
        catch { }
    }

    // ====================== SALAS (HISTORIAS) ======================
    private void LoadRooms()
    {
        if (UserSession.ActiveProject != null)
        {
            UserSession.ActiveProject = _proyectos.FirstOrDefault(p => p.Idproyecto == UserSession.ActiveProject.Idproyecto)
                                        ?? _proyectos.FirstOrDefault();
        }
        else if (_proyectos.Count > 0)
        {
            UserSession.ActiveProject = _proyectos.FirstOrDefault();
        }

        RoomsStack.Children.Clear();

        if (UserSession.EsArquitecto)
            RoomsStack.Children.Add(CreateNewRoomButton());

        foreach (var proyecto in _proyectos)
        {
            bool isActive = UserSession.ActiveProject != null &&
                            UserSession.ActiveProject.Idproyecto == proyecto.Idproyecto;
            RoomsStack.Children.Add(CreateRoomCircle(proyecto, isActive));
        }
    }

    private View CreateNewRoomButton()
    {
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var border = new Border
        {
            WidthRequest = 70,
            HeightRequest = 70,
            BackgroundColor = Color.FromArgb(isDark ? "#1E293B" : "#F1F5F9"),
            Stroke = Color.FromArgb(isDark ? "#334155" : "#CBD5E1"),
            StrokeThickness = 1.5,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 35 },
            Content = new Image
            {
                Source = isDark ? "ic_plus_white.svg" : "ic_plus.svg",
                WidthRequest = 24,
                HeightRequest = 24,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        border.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => OnOpenNewRoomSheetClicked(null, EventArgs.Empty))
        });

        var label = new Label
        {
            Text = "Nueva Sala",
            FontSize = 11,
            TextColor = Color.FromArgb(isDark ? "#94A3B8" : "#64748B"),
            HorizontalTextAlignment = TextAlignment.Center,
            WidthRequest = 72
        };

        var stack = new VerticalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Center, WidthRequest = 72 };
        stack.Children.Add(border);
        stack.Children.Add(label);
        return stack;
    }

    private View CreateRoomCircle(ProyectoDto proyecto, bool isActive)
    {
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        bool tieneSala = !string.IsNullOrWhiteSpace(proyecto.Codigosalaactiva);

        var ringColor = isActive
            ? Color.FromArgb("#38BDF8")
            : (tieneSala
                ? Color.FromArgb("#0EA5E9")
                : Color.FromArgb(isDark ? "#334155" : "#CBD5E1"));

        var innerCircle = new Border
        {
            WidthRequest = 62,
            HeightRequest = 62,
            StrokeThickness = 0,
            BackgroundColor = Color.FromArgb(isDark ? "#1E293B" : "#F1F5F9"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 31 },
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };

        var grid = new Grid();
        grid.Children.Add(new Label
        {
            Text = proyecto.PrimeraLetra,
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb(isDark ? "#F8FAFC" : "#0F172A"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        });

        if (!string.IsNullOrWhiteSpace(proyecto.Imagenurl))
        {
            grid.Children.Add(new Image
            {
                Source = proyecto.Imagenurl,
                Aspect = Aspect.AspectFill,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            });
        }

        innerCircle.Content = grid;

        var outerRing = new Border
        {
            WidthRequest = 70,
            HeightRequest = 70,
            StrokeThickness = isActive ? 3 : 1.5,
            Stroke = ringColor,
            BackgroundColor = Colors.Transparent,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 35 },
            Content = innerCircle
        };

        outerRing.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => OnRoomTapped(proyecto))
        });

        var nameLabel = new Label
        {
            Text = proyecto.Nombre,
            FontSize = 11,
            FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None,
            TextColor = Color.FromArgb(isActive
                ? (isDark ? "#FFFFFF" : "#0F172A")
                : (isDark ? "#94A3B8" : "#64748B")),
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
            WidthRequest = 72
        };

        var stack = new VerticalStackLayout { Spacing = 6, HorizontalOptions = LayoutOptions.Center, WidthRequest = 72 };
        stack.Children.Add(outerRing);
        stack.Children.Add(nameLabel);
        return stack;
    }

    private async void OnRoomTapped(ProyectoDto proyecto)
    {
        UserSession.ActiveProject = proyecto;
        LoadRooms();
        await ShowToastAsync($"Proyecto activo: {proyecto.Nombre}");
    }

    // ====================== FEED & INTERACCIONES ======================
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
        await OpenDetailsSheetAsync();
    }

    private async void OnCardLikeTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ProyectoDto proyecto) return;
        proyecto.EsDestacado = !proyecto.EsDestacado;
        Preferences.Default.Set($"fav_project_{UserSession.Idusuario}_{proyecto.Idproyecto}", proyecto.EsDestacado);

        if (sender is VisualElement ve)
        {
            await ve.ScaleToAsync(1.22, 90, Easing.CubicOut);
            await ve.ScaleToAsync(1.0, 90, Easing.CubicIn);
        }
    }

    private async void OnCardCommentsTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ProyectoDto proyecto) return;
        UserSession.ActiveProject = proyecto;
        LoadRooms();
        await Navigation.PushModalAsync(new SuggestionsPage(proyecto.Idproyecto));
    }

    private async void OnCardShareTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not ProyectoDto proyecto) return;
        UserSession.ActiveProject = proyecto;
        LoadRooms();

        string textoCompartir = !string.IsNullOrWhiteSpace(proyecto.Codigosalaactiva)
            ? $"Proyecto ArchiSpace: {proyecto.Nombre} — Código de sala: {proyecto.Codigosalaactiva}"
            : $"Proyecto ArchiSpace: {proyecto.Nombre} ({proyecto.Ubicacion ?? "En desarrollo"})";

        try
        {
            await Clipboard.Default.SetTextAsync(!string.IsNullOrWhiteSpace(proyecto.Codigosalaactiva)
                ? proyecto.Codigosalaactiva
                : textoCompartir);

            await ShowToastAsync(!string.IsNullOrWhiteSpace(proyecto.Codigosalaactiva)
                ? $"Código {proyecto.Codigosalaactiva} copiado"
                : "Información del proyecto copiada");
        }
        catch { }
    }

    // ====================== PROYECTOS ======================
    private async Task CargarProyectosAsync()
    {
        _proyectos.Clear();
        try
        {
            var list = UserSession.EsArquitecto
                ? await ApiService.GetProyectosByArquitectoAsync(UserSession.Idusuario)
                : await ApiService.GetProyectosByClienteAsync(UserSession.Idusuario);

            foreach (var prj in list)
            {
                prj.EsDestacado = Preferences.Default.Get($"fav_project_{UserSession.Idusuario}_{prj.Idproyecto}", false);
                _proyectos.Add(prj);
            }
        }
        catch { }
    }

    private async Task CargarResumenesActividadAsync()
    {
        var snapshot = _proyectos.ToList();
        foreach (var prj in snapshot)
        {
            try
            {
                var medsTask = ApiService.GetMedicionesByProyectoAsync(prj.Idproyecto);
                var sugsTask = ApiService.GetSugerenciasByProyectoAsync(prj.Idproyecto);
                await Task.WhenAll(medsTask, sugsTask);

                int nMeds = medsTask.Result?.Count ?? 0;
                var sugs = sugsTask.Result ?? new List<SugerenciaDto>();
                int nSugs = sugs.Count;
                int aprobadas = sugs.Count(s => string.Equals(s.Estado, "Aprobado", StringComparison.OrdinalIgnoreCase)
                                             || string.Equals(s.Estado, "Implementado", StringComparison.OrdinalIgnoreCase));

                if (nMeds == 0 && nSugs == 0)
                {
                    prj.ResumenActividad = "Sin mediciones ni sugerencias registradas";
                }
                else
                {
                    var partes = new List<string>();
                    if (nMeds > 0) partes.Add($"{nMeds} {(nMeds == 1 ? "medición" : "mediciones")}");
                    if (nSugs > 0) partes.Add($"{nSugs} {(nSugs == 1 ? "sugerencia" : "sugerencias")}");
                    if (aprobadas > 0) partes.Add($"{aprobadas} {(aprobadas == 1 ? "aprobada" : "aprobadas")}");
                    prj.ResumenActividad = string.Join(" · ", partes);
                }
            }
            catch
            {
                prj.ResumenActividad = "Proyecto sincronizado";
            }
        }
    }

    private async Task CargarNotificacionesDashAsync()
    {
        NotificationsContainer.Children.Clear();
        var list = await ApiService.GetNotificacionesAsync();

        int unreadCount = list?.Count(n => n.Leida != true) ?? 0;
        NotificationCountBadge.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();
        NotificationBadgeBorder.IsVisible = unreadCount > 0;

        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        if (list != null)
        {
            foreach (var n in list.Take(3))
            {
                var textStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
                textStack.Children.Add(new Label
                {
                    Text = n.Mensaje,
                    FontSize = 13,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb(isDark ? "#F8FAFC" : "#001D39")
                });
                textStack.Children.Add(new Label
                {
                    Text = n.FechaFormateada,
                    FontSize = 11,
                    TextColor = Color.FromArgb(isDark ? "#94A3B8" : "#6EA2B3")
                });

                var row = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = GridLength.Star } },
                    Padding = new Thickness(15)
                };
                row.Children.Add(textStack);

                var card = new Border
                {
                    BackgroundColor = Color.FromArgb(isDark ? "#1C1C1E" : "#FFFFFF"),
                    Stroke = Color.FromArgb(isDark ? "#2C2C2E" : "#E2E8F0"),
                    StrokeThickness = 1,
                    Margin = new Thickness(0, 0, 0, 10),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) },
                    Content = row
                };
                card.GestureRecognizers.Add(new TapGestureRecognizer
                {
                    Command = new Command(async () => await Navigation.PushModalAsync(new NotificationsPage()))
                });
                NotificationsContainer.Children.Add(card);
            }
        }
    }

    private async void OnNotificationsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new NotificationsPage());
    }

    // ====================== DETALLE PROYECTO ======================
    private async void OnActiveProjectDetailsClicked(object? sender, EventArgs e) => await OpenDetailsSheetAsync();
    private async void OnCloseDetailsSheetClicked(object? sender, EventArgs e) => await CloseDetailsSheetAsync();
    private async void OnCloseDetailsSheetTapped(object? sender, TappedEventArgs e) => await CloseDetailsSheetAsync();

    private async Task OpenDetailsSheetAsync()
    {
        if (UserSession.ActiveProject == null)
        {
            await AlertService.ShowAlertAsync("Aviso", "Selecciona un proyecto primero.", "OK");
            return;
        }

        var proy = UserSession.ActiveProject;
        SheetProjectName.Text = proy.Nombre;
        SheetProjectDesc.Text = string.IsNullOrWhiteSpace(proy.Descripcion) ? "Sin descripción" : proy.Descripcion;
        SheetProjectUbicacion.Text = string.IsNullOrWhiteSpace(proy.Ubicacion) ? "--" : proy.Ubicacion;
        SheetProjectEstado.Text = string.IsNullOrWhiteSpace(proy.Estado) ? "--" : proy.Estado;
        SheetProjectCode.Text = $"Presupuesto: {(proy.Presupuesto ?? 0m):C}";

        bool esArquitecto = UserSession.EsArquitecto;
        LblRoleCounterpartTitle.Text = esArquitecto ? "Cliente" : "Arquitecto";
        SheetProjectClient.Text = "Cargando...";

        RowInvitarCliente.IsVisible = esArquitecto;
        SepInvitarCliente.IsVisible = esArquitecto;
        SectionAdmin.IsVisible = esArquitecto;

        await UiHelper.OpenSheetAsync(DetailsBackdrop, ProjectDetailsSheetModal);

        int? idContraparte = esArquitecto ? proy.Idcliente : proy.Idarquitecto;
        if (idContraparte.HasValue && idContraparte.Value > 0)
        {
            var u = await ApiService.GetUsuarioByIdAsync(idContraparte.Value);
            SheetProjectClient.Text = u != null ? u.NombreCompleto : (esArquitecto ? "Sin asignar" : "Arquitecto asignado");
        }
        else
        {
            SheetProjectClient.Text = esArquitecto ? "Sin asignar" : "No especificado";
        }
    }

    private Task CloseDetailsSheetAsync() =>
        UiHelper.CloseSheetAsync(DetailsBackdrop, ProjectDetailsSheetModal);

    private async void OnEnterDesignClicked(object? sender, EventArgs e)
    {
        await CloseDetailsSheetAsync();
        await Shell.Current.GoToAsync("//DesignPage");
    }

    // ====================== EDITAR PROYECTO ======================
    private async void OnEditarProyectoFromDetailsClicked(object? sender, EventArgs e)
    {
        await CloseDetailsSheetAsync();
        if (UserSession.ActiveProject == null) return;

        var proy = UserSession.ActiveProject;
        _imagenEditProyectoSeleccionada = null;
        LblNombreImagenEditProyecto.IsVisible = false;

        if (!string.IsNullOrWhiteSpace(proy.Imagenurl))
        {
            ImgPreviewEditProyecto.Source = proy.Imagenurl;
            ImgPreviewEditProyecto.IsVisible = true;
            ImgPlaceholderEditProyecto.IsVisible = false;
        }
        else
        {
            ImgPreviewEditProyecto.Source = null;
            ImgPreviewEditProyecto.IsVisible = false;
            ImgPlaceholderEditProyecto.IsVisible = true;
        }

        EditNombreProyecto.Text = proy.Nombre;
        EditDescripcionProyecto.Text = proy.Descripcion;
        EditUbicacionProyecto.Text = proy.Ubicacion;
        EditPresupuestoProyecto.Text = (proy.Presupuesto ?? 0m).ToString("0.##", CultureInfo.InvariantCulture);

        EditEstadoProyecto.SelectedItem = !string.IsNullOrWhiteSpace(proy.Estado) && EditEstadoProyecto.Items.Contains(proy.Estado)
            ? proy.Estado
            : "Borrador";

        await CargarClientesParaEditPickerAsync(proy.Idcliente);
        await UiHelper.OpenSheetAsync(EditProjectBackdrop, EditProjectSheetModal);
    }

    private async Task CargarClientesParaEditPickerAsync(int? idClienteActual)
    {
        try
        {
            var usuarios = await ApiService.GetUsuariosAsync();
            var opciones = new List<UsuarioDto>
            {
                new UsuarioDto { Idusuario = 0, Nombre = "Sin", Apellido = "asignar" }
            };
            var clientes = usuarios
                .Where(u => string.Equals(u.Rol, "Cliente", StringComparison.OrdinalIgnoreCase))
                .ToList();

            opciones.AddRange(clientes);
            EditPickerClienteProyecto.ItemsSource = opciones;

            EditPickerClienteProyecto.SelectedItem = (idClienteActual.HasValue && idClienteActual.Value > 0)
                ? opciones.FirstOrDefault(c => c.Idusuario == idClienteActual.Value) ?? opciones[0]
                : opciones[0];
        }
        catch
        {
            EditPickerClienteProyecto.ItemsSource = new List<UsuarioDto>();
        }
    }

    private async void OnCloseEditProjectSheetClicked(object? sender, TappedEventArgs e) => await CloseEditProjectSheetAsync();
    private async void OnCloseEditProjectSheetTapped(object? sender, TappedEventArgs e) => await CloseEditProjectSheetAsync();

    private async Task CloseEditProjectSheetAsync()
    {
        await UiHelper.CloseSheetAsync(EditProjectBackdrop, EditProjectSheetModal);
        _imagenEditProyectoSeleccionada = null;
    }

    private async void OnSeleccionarImagenEditarProyectoClicked(object? sender, TappedEventArgs e)
    {
        try
        {
            var resultado = await UiHelper.PickValidatedImageAsync("Selecciona una nueva imagen");
            if (resultado == null) return;

            _imagenEditProyectoSeleccionada = resultado;

            var stream = await resultado.OpenReadAsync();
            ImgPreviewEditProyecto.Source = ImageSource.FromStream(() => stream);
            ImgPreviewEditProyecto.IsVisible = true;
            ImgPlaceholderEditProyecto.IsVisible = false;
            LblNombreImagenEditProyecto.Text = resultado.FileName;
            LblNombreImagenEditProyecto.IsVisible = true;
        }
        catch (Exception ex)
        {
            await AlertService.ShowAlertAsync("Error", $"No se pudo seleccionar la imagen: {ex.Message}", "OK");
        }
    }

    private async void OnSubmitEditarProyectoClicked(object? sender, EventArgs e) => await SubmitEditarProyectoAsync();
    private async void OnSubmitEditarProyectoClicked(object? sender, TappedEventArgs e) => await SubmitEditarProyectoAsync();

    private async Task SubmitEditarProyectoAsync()
    {
        if (UserSession.ActiveProject == null) return;

        var p = UserSession.ActiveProject;
        int? idCliente = p.Idcliente;
        if (EditPickerClienteProyecto.SelectedItem is UsuarioDto selCli)
        {
            idCliente = selCli.Idusuario <= 0 ? 0 : selCli.Idusuario;
        }

        var req = new ActualizarProyectoRequest
        {
            Idproyecto = p.Idproyecto,
            Idcliente = idCliente,
            Nombre = string.IsNullOrWhiteSpace(EditNombreProyecto.Text) ? p.Nombre : EditNombreProyecto.Text.Trim(),
            Descripcion = EditDescripcionProyecto.Text?.Trim() ?? p.Descripcion,
            Ubicacion = EditUbicacionProyecto.Text?.Trim() ?? p.Ubicacion,
            Presupuesto = UiHelper.ParseDecimal(EditPresupuestoProyecto.Text, p.Presupuesto ?? 0m),
            Estado = EditEstadoProyecto.SelectedItem?.ToString() ?? p.Estado
        };

        var res = await ApiService.ActualizarProyectoAsync(p.Idproyecto, req);
        if (!res.Success)
        {
            await AlertService.ShowAlertAsync("Error", res.Message, "OK");
            return;
        }

        p.Nombre = req.Nombre;
        p.Descripcion = req.Descripcion;
        p.Ubicacion = req.Ubicacion;
        p.Presupuesto = req.Presupuesto;
        p.Estado = req.Estado;
        p.Idcliente = (idCliente.HasValue && idCliente.Value <= 0) ? null : idCliente;

        if (_imagenEditProyectoSeleccionada != null)
        {
            var (okImg, msgImg, urlImg) = await ApiService.SubirImagenProyectoAsync(p.Idproyecto, _imagenEditProyectoSeleccionada);
            if (okImg && !string.IsNullOrEmpty(urlImg))
                p.Imagenurl = urlImg;
            else if (!okImg)
                await AlertService.ShowAlertAsync("Imagen no actualizada",
                    $"Los datos se guardaron, pero la imagen no se pudo subir:\n{msgImg}", "OK");
        }

        await CloseEditProjectSheetAsync();
        await CargarProyectosAsync();
        LoadRooms();
        _ = CargarResumenesActividadAsync();
        await ShowToastAsync("Proyecto actualizado");
    }

    private async void OnEliminarProyectoFromDetailsClicked(object? sender, EventArgs e)
    {
        var confirm = await AlertService.ShowAlertAsync("Eliminar Proyecto", "¿Estás seguro de eliminar este proyecto de forma permanente?", "Eliminar", "Cancelar");
        if (!confirm || UserSession.ActiveProject == null) return;

        var res = await ApiService.EliminarProyectoAsync(UserSession.ActiveProject.Idproyecto);
        if (res.Success)
        {
            UserSession.ActiveProject = null;
            await CloseDetailsSheetAsync();
            await CargarProyectosAsync();
            LoadRooms();
            _ = CargarResumenesActividadAsync();
        }
        else
        {
            await AlertService.ShowAlertAsync("Error al Eliminar", res.Message, "OK");
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
        await UiHelper.OpenSheetAsync(NewProjectBackdrop, NewProjectSheetCard);
    }

    private async Task CargarClientesParaPickerAsync()
    {
        try
        {
            var usuarios = await ApiService.GetUsuariosAsync();
            _clientesCache = usuarios
                .Where(u => string.Equals(u.Rol, "Cliente", StringComparison.OrdinalIgnoreCase))
                .ToList();

            PickerClienteProyecto.ItemsSource = _clientesCache;
            LblSinClientes.IsVisible = _clientesCache.Count == 0;
            PickerClienteProyecto.IsEnabled = _clientesCache.Count > 0;
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
            var resultado = await UiHelper.PickValidatedImageAsync("Selecciona una imagen para el proyecto");
            if (resultado == null) return;

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
            await AlertService.ShowAlertAsync("Error", $"No se pudo seleccionar la imagen: {ex.Message}", "OK");
        }
    }

    private async void OnCloseNewProjectSheetClicked(object? sender, EventArgs e) => await CloseNewProjectSheetAsync();

    private async Task CloseNewProjectSheetAsync()
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

        await UiHelper.CloseSheetAsync(NewProjectBackdrop, NewProjectSheetCard, 400);
    }

    private async void OnSubmitCrearProyectoClicked(object? sender, EventArgs e) => await SubmitCrearProyectoAsync(sender);
    private async void OnSubmitCrearProyectoClicked(object? sender, TappedEventArgs e) => await SubmitCrearProyectoAsync(sender);

    private async Task SubmitCrearProyectoAsync(object? sender)
    {
        if (string.IsNullOrWhiteSpace(EntryNombreProyecto.Text))
        {
            await AlertService.ShowAlertAsync("Falta el nombre", "El nombre del proyecto es obligatorio.", "OK");
            return;
        }

        decimal presupuesto = UiHelper.ParseDecimal(EntryPresupuestoProyecto.Text, 0m);

        int? idCliente = null;
        if (PickerClienteProyecto.SelectedItem is UsuarioDto cliente && cliente.Idusuario > 0)
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
                ? "Proyecto arquitectónico"
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
                await AlertService.ShowAlertAsync("Error", res.Message, "OK");
                return;
            }

            if (_imagenProyectoSeleccionada != null)
            {
                var (okImg, msgImg, _) = await ApiService.SubirImagenProyectoAsync(res.Data.Idproyecto, _imagenProyectoSeleccionada);
                if (!okImg)
                    await AlertService.ShowAlertAsync("Proyecto creado",
                        $"El proyecto se creó, pero la imagen no se pudo subir:\n{msgImg}", "OK");
            }

            await CloseNewProjectSheetAsync();
            await CargarProyectosAsync();
            UserSession.ActiveProject = _proyectos.FirstOrDefault(pr => pr.Idproyecto == res.Data.Idproyecto) ?? _proyectos.FirstOrDefault();
            LoadRooms();
            _ = CargarResumenesActividadAsync();
            _ = ConectarSalasSignalRAsync();
            await ShowToastAsync($"Proyecto '{p.Nombre}' creado");
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
        await UiHelper.OpenSheetAsync(NewRoomBackdrop, NewRoomSheetModal);
    }

    private async Task CargarProyectosSinSalaAsync()
    {
        await CargarProyectosAsync();
        var sinSala = _proyectos.Where(p => string.IsNullOrWhiteSpace(p.Codigosalaactiva)).ToList();

        PickerProyectoSinSala.ItemsSource = sinSala;
        PickerProyectoSinSala.SelectedItem = null;
        NewRoomNombreEntry.Text = "";

        LblSinProyectos.IsVisible = sinSala.Count == 0;
        PickerProyectoSinSala.IsEnabled = sinSala.Count > 0;
    }

    private void OnProyectoSinSalaSelected(object? sender, EventArgs e)
    {
        if (PickerProyectoSinSala.SelectedItem is ProyectoDto proyecto && string.IsNullOrWhiteSpace(NewRoomNombreEntry.Text))
        {
            NewRoomNombreEntry.Text = proyecto.Nombre;
        }
    }

    private async void OnCloseNewRoomSheetClicked(object? sender, TappedEventArgs e) => await CloseNewRoomSheetAsync();

    private Task CloseNewRoomSheetAsync() =>
        UiHelper.CloseSheetAsync(NewRoomBackdrop, NewRoomSheetModal, 400);

    private async void OnSubmitNewRoomClicked(object? sender, EventArgs e) => await SubmitNewRoomAsync();
    private async void OnSubmitNewRoomClicked(object? sender, TappedEventArgs e) => await SubmitNewRoomAsync();

    private async Task SubmitNewRoomAsync()
    {
        if (PickerProyectoSinSala.SelectedItem is not ProyectoDto proyectoSeleccionado)
        {
            await AlertService.ShowAlertAsync("Aviso", "Selecciona un proyecto para activar su sala.", "OK");
            return;
        }

        var (okSala, codigoSala) = await ApiService.ActivarSalaAsync(proyectoSeleccionado.Idproyecto);
        string codigoFinal = okSala && !string.IsNullOrWhiteSpace(codigoSala)
            ? codigoSala
            : $"ARQ-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";

        var (okInv, msgInv, data) = await ApiService.CrearInvitacionAsync(proyectoSeleccionado.Idproyecto, codigoFinal);
        string codigoMostrar = data?.Codigo ?? codigoFinal;

        if (okSala || okInv)
        {
            await CloseNewRoomSheetAsync();
            NewRoomNombreEntry.Text = "";
            PickerProyectoSinSala.SelectedItem = null;

            await CargarProyectosAsync();
            UserSession.ActiveProject = _proyectos.FirstOrDefault(p => p.Idproyecto == proyectoSeleccionado.Idproyecto) ?? UserSession.ActiveProject;
            LoadRooms();
            _ = ConectarSalasSignalRAsync();

            await Clipboard.Default.SetTextAsync(codigoMostrar);
            await AlertService.ShowAlertAsync("Sala Activada",
                $"Código de invitación copiado al portapapeles:\n\n{codigoMostrar}\n\nCompártelo con tu cliente para que se vincule al proyecto.",
                "Listo");
        }
        else
        {
            await AlertService.ShowAlertAsync("Error", $"No se pudo activar la sala: {msgInv}", "OK");
        }
    }

    // ====================== UNIRSE CON CÓDIGO ======================
    private async void OnOpenJoinCodeSheetClicked(object? sender, EventArgs e)
    {
        await UiHelper.OpenSheetAsync(JoinCodeBackdrop, JoinCodeSheetModal);
    }

    private async void OnCloseJoinCodeSheetClicked(object? sender, TappedEventArgs e) => await CloseJoinCodeSheetAsync();

    private Task CloseJoinCodeSheetAsync() =>
        UiHelper.CloseSheetAsync(JoinCodeBackdrop, JoinCodeSheetModal, 400);

    private async void OnSubmitJoinCodeClicked(object? sender, EventArgs e) => await SubmitJoinCodeAsync();
    private async void OnSubmitJoinCodeClicked(object? sender, TappedEventArgs e) => await SubmitJoinCodeAsync();

    private async Task SubmitJoinCodeAsync()
    {
        var codigo = JoinCodeEntry.Text?.Trim().ToUpperInvariant() ?? "";
        if (string.IsNullOrEmpty(codigo))
        {
            await AlertService.ShowAlertAsync("Código requerido", "Ingresa el código que te compartió tu arquitecto.", "Entendido");
            return;
        }

        var (success, msg) = await ApiService.UsarInvitacionAsync(codigo);
        if (success)
        {
            JoinCodeEntry.Text = "";
            await CloseJoinCodeSheetAsync();
            await CargarProyectosAsync();
            LoadRooms();
            _ = CargarResumenesActividadAsync();
            _ = ConectarSalasSignalRAsync();
            await ShowToastAsync("Proyecto vinculado con éxito");
        }
        else
        {
            await AlertService.ShowAlertAsync("Error al vincular", msg, "OK");
        }
    }

    private async void OnGenerarInvitacionClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;

        var proy = UserSession.ActiveProject;
        var (okSala, codSala) = await ApiService.ActivarSalaAsync(proy.Idproyecto);
        string codigoGenerado = okSala && !string.IsNullOrWhiteSpace(codSala)
            ? codSala
            : $"ARQ-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";

        var (success, msg, data) = await ApiService.CrearInvitacionAsync(proy.Idproyecto, codigoGenerado);
        string codigoFinal = data?.Codigo ?? codigoGenerado;

        if (okSala || success)
        {
            await Clipboard.Default.SetTextAsync(codigoFinal);
            await CargarProyectosAsync();
            LoadRooms();
            await AlertService.ShowAlertAsync("Código Copiado",
                $"Comparte este código con tu cliente (ya está copiado en tu portapapeles):\n\n{codigoFinal}", "Listo");
        }
        else
        {
            await AlertService.ShowAlertAsync("Error", $"No se pudo generar la invitación: {msg}", "OK");
        }
    }

    // ====================== ESPACIOS / SUGERENCIAS / MEDICIONES ======================
    private async void OnVerEspaciosClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;
        await CloseDetailsSheetAsync();
        await Navigation.PushModalAsync(new SpacesPage(UserSession.ActiveProject.Idproyecto));
    }

    private async void OnVerSugerenciasClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;
        await CloseDetailsSheetAsync();
        await Navigation.PushModalAsync(new SuggestionsPage(UserSession.ActiveProject.Idproyecto));
    }

    private async void OnVerMedicionesClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;
        await CloseDetailsSheetAsync();
        await Navigation.PushModalAsync(new MeasurementsPage(UserSession.ActiveProject.Idproyecto));
    }

    // ====================== HELPERS ======================
    private Task ShowToastAsync(string message) =>
        UiHelper.ShowToastAsync(AppleToast, AppleToastMessage, message, 3000);
}
