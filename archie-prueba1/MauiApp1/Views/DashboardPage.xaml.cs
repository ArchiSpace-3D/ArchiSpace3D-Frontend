using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

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
            var actualizado = _proyectos.FirstOrDefault(p => p.Idproyecto == UserSession.ActiveProject.Idproyecto);
            if (actualizado != null)
                UserSession.ActiveProject = actualizado;
            else
                UserSession.ActiveProject = _proyectos.FirstOrDefault();
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
            List<ProyectoDto> list = UserSession.EsArquitecto
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
                var medsTask = ApiService.GetMediciónesByProyectoAsync(prj.Idproyecto);
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
            await ShowAlertAsync("Aviso", "Selecciona un proyecto primero.", "OK");
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

        DetailsBackdrop.IsVisible = true;
        await DetailsBackdrop.FadeToAsync(1, 200);
        ProjectDetailsSheetModal.IsVisible = true;
        await Task.WhenAll(
            ProjectDetailsSheetModal.FadeToAsync(1, 250),
            ProjectDetailsSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut)
        );

        if (esArquitecto)
        {
            var idCliente = proy.Idcliente;
            if (idCliente.HasValue && idCliente.Value > 0)
            {
                var cliente = await ApiService.GetUsuarioByIdAsync(idCliente.Value);
                SheetProjectClient.Text = cliente != null ? cliente.NombreCompleto : "Sin asignar";
            }
            else
            {
                SheetProjectClient.Text = "Sin asignar";
            }
        }
        else
        {
            var idArq = proy.Idarquitecto;
            if (idArq.HasValue && idArq.Value > 0)
            {
                var arq = await ApiService.GetUsuarioByIdAsync(idArq.Value);
                SheetProjectClient.Text = arq != null ? arq.NombreCompleto : "Arquitecto asignado";
            }
            else
            {
                SheetProjectClient.Text = "No especificado";
            }
        }
    }

    private async Task CloseDetailsSheetAsync()
    {
        await Task.WhenAll(
            ProjectDetailsSheetModal.FadeToAsync(0, 200),
            ProjectDetailsSheetModal.TranslateToAsync(0, 500, 250, Easing.CubicIn)
        );
        ProjectDetailsSheetModal.IsVisible = false;
        await DetailsBackdrop.FadeToAsync(0, 200);
        DetailsBackdrop.IsVisible = false;
    }

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

        if (!string.IsNullOrWhiteSpace(proy.Estado) && EditEstadoProyecto.Items.Contains(proy.Estado))
            EditEstadoProyecto.SelectedItem = proy.Estado;
        else
            EditEstadoProyecto.SelectedItem = "Borrador";

        await CargarClientesParaEditPickerAsync(proy.Idcliente);

        EditProjectBackdrop.IsVisible = true;
        await EditProjectBackdrop.FadeToAsync(1, 200);
        EditProjectSheetModal.IsVisible = true;
        await Task.WhenAll(
            EditProjectSheetModal.FadeToAsync(1, 250),
            EditProjectSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut)
        );
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
            var clientes = usuarios?
                .Where(u => string.Equals(u.Rol, "Cliente", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<UsuarioDto>();

            opciones.AddRange(clientes);
            EditPickerClienteProyecto.ItemsSource = opciones;

            if (idClienteActual.HasValue && idClienteActual.Value > 0)
            {
                EditPickerClienteProyecto.SelectedItem = opciones.FirstOrDefault(c => c.Idusuario == idClienteActual.Value) ?? opciones[0];
            }
            else
            {
                EditPickerClienteProyecto.SelectedItem = opciones[0];
            }
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
        await Task.WhenAll(
            EditProjectSheetModal.FadeToAsync(0, 200),
            EditProjectSheetModal.TranslateToAsync(0, 500, 250, Easing.CubicIn)
        );
        EditProjectSheetModal.IsVisible = false;
        await EditProjectBackdrop.FadeToAsync(0, 200);
        EditProjectBackdrop.IsVisible = false;
        _imagenEditProyectoSeleccionada = null;
    }

    private async void OnSeleccionarImagenEditarProyectoClicked(object? sender, TappedEventArgs e)
    {
        try
        {
            var resultado = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona una nueva imagen",
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
            await ShowAlertAsync("Error", $"No se pudo seleccionar la imagen: {ex.Message}", "OK");
        }
    }

    private async void OnSubmitEditarProyectoClicked(object? sender, EventArgs e) => await SubmitEditarProyectoAsync();
    private async void OnSubmitEditarProyectoClicked(object? sender, TappedEventArgs e) => await SubmitEditarProyectoAsync();

    private static decimal ParsePresupuestoSeguro(string? raw, decimal fallback = 0m)
    {
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        string clean = raw.Replace("$", "").Trim();
        if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            return val;
        if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.CurrentCulture, out val))
            return val;
        return fallback;
    }

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
            Presupuesto = ParsePresupuestoSeguro(EditPresupuestoProyecto.Text, p.Presupuesto ?? 0m),
            Estado = EditEstadoProyecto.SelectedItem?.ToString() ?? p.Estado
        };

        var res = await ApiService.ActualizarProyectoAsync(p.Idproyecto, req);
        if (!res.Success)
        {
            ShowCustomAlert("Error", res.Message, true);
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
                await ShowAlertAsync("Imagen no actualizada",
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
        var confirm = await ShowAlertConfirmAsync("Eliminar Proyecto", "¿Estás seguro de eliminar este proyecto de forma permanente?", "Eliminar", "Cancelar");
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
            _clientesCache = usuarios?
                .Where(u => string.Equals(u.Rol, "Cliente", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<UsuarioDto>();

            PickerClienteProyecto.ItemsSource = _clientesCache;

            if (_clientesCache.Count == 0)
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

        await Task.WhenAll(
            NewProjectSheetCard.FadeToAsync(0, 200),
            NewProjectSheetCard.TranslateToAsync(0, 400, 250, Easing.CubicIn)
        );
        NewProjectSheetCard.IsVisible = false;
        await NewProjectBackdrop.FadeToAsync(0, 200);
        NewProjectBackdrop.IsVisible = false;
    }

    private async void OnSubmitCrearProyectoClicked(object? sender, EventArgs e) => await SubmitCrearProyectoAsync(sender);
    private async void OnSubmitCrearProyectoClicked(object? sender, TappedEventArgs e) => await SubmitCrearProyectoAsync(sender);

    private async Task SubmitCrearProyectoAsync(object? sender)
    {
        if (string.IsNullOrWhiteSpace(EntryNombreProyecto.Text))
        {
            ShowCustomAlert("Falta el nombre", "El nombre del proyecto es obligatorio.", true);
            return;
        }

        decimal presupuesto = ParsePresupuestoSeguro(EntryPresupuestoProyecto.Text, 0m);

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
                ShowCustomAlert("Error", res.Message, true);
                return;
            }

            if (_imagenProyectoSeleccionada != null)
            {
                var (okImg, msgImg, _) = await ApiService.SubirImagenProyectoAsync(res.Data.Idproyecto, _imagenProyectoSeleccionada);
                if (!okImg)
                    await ShowAlertAsync("Proyecto creado",
                        $"El proyecto se creó, pero la imagen no se pudo subir:\n{msgImg}", "OK");
            }

            await CloseNewProjectSheetAsync();
            await CargarProyectosAsync();
            UserSession.ActiveProject = _proyectos.FirstOrDefault(pr => pr.Idproyecto == res.Data.Idproyecto) ?? _proyectos.FirstOrDefault();
            LoadRooms();
            _ = CargarResumenesActividadAsync();
            _ = ConectarSalasSignalRAsync();
            ShowCustomAlert("Proyecto Creado", $"El proyecto '{p.Nombre}' ha sido guardado exitosamente.", false);
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
        var sinSala = _proyectos.Where(p => string.IsNullOrWhiteSpace(p.Codigosalaactiva)).ToList();

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

    private async void OnCloseNewRoomSheetClicked(object? sender, TappedEventArgs e) => await CloseNewRoomSheetAsync();

    private async Task CloseNewRoomSheetAsync()
    {
        await Task.WhenAll(
            NewRoomSheetModal.FadeToAsync(0, 200),
            NewRoomSheetModal.TranslateToAsync(0, 400, 250, Easing.CubicIn)
        );
        NewRoomSheetModal.IsVisible = false;
        await NewRoomBackdrop.FadeToAsync(0, 200);
        NewRoomBackdrop.IsVisible = false;
    }

    private async void OnSubmitNewRoomClicked(object? sender, EventArgs e) => await SubmitNewRoomAsync();
    private async void OnSubmitNewRoomClicked(object? sender, TappedEventArgs e) => await SubmitNewRoomAsync();

    private async Task SubmitNewRoomAsync()
    {
        if (PickerProyectoSinSala.SelectedItem is not ProyectoDto proyectoSeleccionado)
        {
            await ShowAlertAsync("Aviso", "Selecciona un proyecto para activar su sala.", "OK");
            return;
        }

        // 1. Activar sala en Proyecto para que Codigosalaactiva quede persistido
        var (okSala, codigoSala) = await ApiService.ActivarSalaAsync(proyectoSeleccionado.Idproyecto);
        string codigoFinal = okSala && !string.IsNullOrWhiteSpace(codigoSala)
            ? codigoSala
            : $"ARQ-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";

        // 2. Registrar invitación vinculable por código para el Cliente
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
            await ShowAlertAsync("Sala Activada",
                $"Código de invitación copiado al portapapeles:\n\n{codigoMostrar}\n\nCompártelo con tu cliente para que se vincule al proyecto.",
                "Listo");
        }
        else
        {
            await ShowAlertAsync("Error", $"No se pudo activar la sala: {msgInv}", "OK");
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
            JoinCodeSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut)
        );
    }

    private async void OnCloseJoinCodeSheetClicked(object? sender, TappedEventArgs e) => await CloseJoinCodeSheetAsync();

    private async Task CloseJoinCodeSheetAsync()
    {
        await Task.WhenAll(
            JoinCodeSheetModal.FadeToAsync(0, 200),
            JoinCodeSheetModal.TranslateToAsync(0, 400, 250, Easing.CubicIn)
        );
        JoinCodeSheetModal.IsVisible = false;
        await JoinCodeBackdrop.FadeToAsync(0, 200);
        JoinCodeBackdrop.IsVisible = false;
    }

    private async void OnSubmitJoinCodeClicked(object? sender, EventArgs e) => await SubmitJoinCodeAsync();
    private async void OnSubmitJoinCodeClicked(object? sender, TappedEventArgs e) => await SubmitJoinCodeAsync();

    private async Task SubmitJoinCodeAsync()
    {
        var codigo = JoinCodeEntry.Text?.Trim().ToUpperInvariant() ?? "";
        if (string.IsNullOrEmpty(codigo))
        {
            await ShowAlertAsync("Código requerido", "Ingresa el código que te compartió tu arquitecto.", "Entendido");
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
            await ShowAlertAsync("Error al vincular", msg, "OK");
        }
    }

    private async void OnGenerarInvitacionClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;

        var proy = UserSession.ActiveProject;
        string codigoGenerado;

        if (string.IsNullOrWhiteSpace(proy.Codigosalaactiva))
        {
            var (okSala, codSala) = await ApiService.ActivarSalaAsync(proy.Idproyecto);
            codigoGenerado = okSala && !string.IsNullOrWhiteSpace(codSala)
                ? codSala
                : $"ARQ-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";
        }
        else
        {
            codigoGenerado = $"ARQ-{Guid.NewGuid().ToString()[..6].ToUpperInvariant()}";
        }

        var (success, msg, data) = await ApiService.CrearInvitacionAsync(proy.Idproyecto, codigoGenerado);

        if (success && data != null)
        {
            await Clipboard.Default.SetTextAsync(data.Codigo);
            await CargarProyectosAsync();
            LoadRooms();
            await ShowAlertAsync("Código Copiado",
                $"Comparte este código con tu cliente (ya está copiado en tu portapapeles):\n\n{data.Codigo}", "Listo");
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
        await CloseDetailsSheetAsync();
        await Navigation.PushModalAsync(new SpacesPage(UserSession.ActiveProject.Idproyecto));
    }

    private async void OnVerSugerenciasClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;
        await CloseDetailsSheetAsync();
        await Navigation.PushModalAsync(new SuggestionsPage(UserSession.ActiveProject.Idproyecto));
    }

    // ====================== MEDICIONES ======================
    private async void OnVerMedicionesClicked(object? sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;

        await CloseDetailsSheetAsync();

        MeasurementsBackdrop.IsVisible = true;
        await MeasurementsBackdrop.FadeToAsync(1, 200);
        MeasurementsSheetModal.IsVisible = true;
        await Task.WhenAll(
            MeasurementsSheetModal.FadeToAsync(1, 250),
            MeasurementsSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut)
        );

        LoadingMeasurementsIndicator.IsVisible = true;
        LoadingMeasurementsIndicator.IsRunning = true;
        MeasurementsCollectionView.ItemsSource = null;

        var mediciones = await ApiService.GetMediciónesByProyectoAsync(UserSession.ActiveProject.Idproyecto);

        LoadingMeasurementsIndicator.IsRunning = false;
        LoadingMeasurementsIndicator.IsVisible = false;

        MeasurementsCollectionView.ItemsSource = mediciones;
    }

    private async void OnCloseMeasurementsSheetClicked(object? sender, EventArgs e) => await CloseMeasurementsSheetAsync();
    private async void OnCloseMeasurementsSheetClicked(object? sender, TappedEventArgs e) => await CloseMeasurementsSheetAsync();

    private async Task CloseMeasurementsSheetAsync()
    {
        await Task.WhenAll(
            MeasurementsSheetModal.FadeToAsync(0, 200),
            MeasurementsSheetModal.TranslateToAsync(0, 500, 250, Easing.CubicIn)
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
                AlertIconImage.Source = "ic_error.svg";
                AlertButton.BackgroundColor = Color.FromArgb("#FFB3C6");
                AlertButton.Text = "Cerrar";
            }
            else
            {
                AlertIconBox.BackgroundColor = Color.FromArgb("#D1F4E0");
                AlertIconImage.Source = "ic_check.svg";
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

    private async void OnCloseAlertClicked(object? sender, EventArgs e) => await CloseAlertAsync();
    private async void OnCloseAlertClicked(object? sender, TappedEventArgs e) => await CloseAlertAsync();

    private async Task CloseAlertAsync()
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
