using MauiApp1.Models;
using MauiApp1.Services;
using System.Text;
using System.Text.Json;
using System.Web;

namespace MauiApp1.Views;

public partial class DesignPage : ContentPage
{
    private bool _is3DMode = false;
    private bool _isOrthoEnabled = false;
    private bool _isWebViewReady = false;
    private int? _loadedProjectId = null;
    private string? _pendingBase64Plan = null;

    // CLIENTE
    private bool _pickMode = false;
    private bool _enviando = false;
    private string _versionTexto = "";
    private string? _zonaNombre, _zonaTipo, _zonaX, _zonaY;
    private FileResult? _fotoSolicitud;

    public DesignPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        ActualizarEncabezadoYPermisos();

        if (!_isWebViewReady)
        {
            await LoadLocalCadViewerAsync();
            return;
        }

        SyncThemeAndRoleToJs();

        int? currentProjId = UserSession.ActiveProject?.Idproyecto;
        if (_loadedProjectId != currentProjId)
        {
            await CargarPlanoProyectoActivoAsync();
        }
    }

    private void ActualizarEncabezadoYPermisos()
    {
        bool esArquitecto = UserSession.EsArquitecto;

        ProjectTitleLabel.Text = UserSession.ActiveProject != null
            ? UserSession.ActiveProject.Nombre
            : "Editor de Espacios";

        string sufijoRol = esArquitecto ? "" : " • Solo lectura";
        ModoLabel.Text = (_is3DMode ? "Modo: 3D Vista" : "Modo: 2D Plano") + sufijoRol;

        ToolbarScroll.IsVisible = esArquitecto && !_is3DMode;
        BtnUndo.IsVisible = esArquitecto && !_is3DMode;
        BtnGuardarPlano.IsVisible = esArquitecto;

        if (!esArquitecto)
        {
            ContextTitle.Text = _is3DMode ? "Vista 3D Interactiva" : "Vista de Plano 2D";
            ContextDesc.Text = _is3DMode
                ? "Gira con un dedo y haz zoom con dos dedos para explorar el modelo."
                : "Desliza con un dedo o haz zoom con dos dedos. Usa Historial para ver versiones.";
        }

        // CLIENTE: interfaz propia; el arquitecto conserva la suya
        ArchitectTopBar.IsVisible = esArquitecto;
        ContextPanel.IsVisible = esArquitecto;
        ClientTopBar.IsVisible = !esArquitecto;
        ClientFabs.IsVisible = !esArquitecto;

        if (!esArquitecto)
        {
            ClientTitleLabel.Text = UserSession.ActiveProject != null
                ? UserSession.ActiveProject.Nombre
                : "Sin proyecto";
            BtnClient3D.Source = _is3DMode ? "ic_client_2d.svg" : "ic_client_3d.svg";
            BtnClientSugerir.IsVisible = !_is3DMode;
            if (_is3DMode) CerrarPanelSolicitud();
            ActualizarSubtituloCliente();
        }
    }

    private async Task LoadLocalCadViewerAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("cad_editor.html");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            var html = await reader.ReadToEndAsync();

            ViewerWebView.Source = new HtmlWebViewSource { Html = html };
        }
        catch (Exception ex)
        {
            await AlertService.ShowAlertAsync("Error", "No se pudo iniciar el editor CAD: " + ex.Message);
        }
    }

    // CLIENTE: recibe archie://zone?... desde el HTML
    private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Url) ||
            !e.Url.StartsWith("archie://", StringComparison.OrdinalIgnoreCase))
            return;

        e.Cancel = true;
        if (UserSession.EsArquitecto) return;

        try
        {
            var uri = new Uri(e.Url);
            if (!uri.Host.Equals("zone", StringComparison.OrdinalIgnoreCase)) return;

            var q = HttpUtility.ParseQueryString(uri.Query);
            string? name = q["name"], type = q["type"], x = q["x"], y = q["y"];
            MainThread.BeginInvokeOnMainThread(() => AbrirPanelSolicitud(name, type, x, y));
        }
        catch { }
    }

    private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        _isWebViewReady = true;
        SyncThemeAndRoleToJs();

        if (UserSession.EsArquitecto)
        {
            UpdateActiveButton(BtnModoLibre);
        }

        if (_pendingBase64Plan != null)
        {
            ViewerWebView.Eval($"if(window.loadDataBase64) window.loadDataBase64('{_pendingBase64Plan}');");
            _pendingBase64Plan = null;
        }
        else
        {
            await CargarPlanoProyectoActivoAsync();
        }
    }

    private void SyncThemeAndRoleToJs()
    {
        if (!_isWebViewReady) return;
        bool isDark = Application.Current?.UserAppTheme == AppTheme.Dark ||
                      (Application.Current?.UserAppTheme == AppTheme.Unspecified && Application.Current?.RequestedTheme == AppTheme.Dark);

        ViewerWebView.Eval($"if(window.setTheme) window.setTheme({isDark.ToString().ToLowerInvariant()});");
        ViewerWebView.Eval($"if(window.setReadOnly) window.setReadOnly({(!UserSession.EsArquitecto).ToString().ToLowerInvariant()});");
    }

    private async Task CargarPlanoProyectoActivoAsync()
    {
        int? projId = UserSession.ActiveProject?.Idproyecto;
        _loadedProjectId = projId;

        if (!UserSession.EsArquitecto) _ = ActualizarVersionClienteAsync(); // CLIENTE

        if (projId == null)
        {
            if (_isWebViewReady)
                ViewerWebView.Eval("if(window.clearCanvas) window.clearCanvas();");
            return;
        }

        try
        {
            string? planJson = await ApiService.ObtenerPlanoProyectoAsync(projId.Value);

            if (ApiService.EsJsonPlanoValido(planJson))
            {
                string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(planJson!));
                if (_isWebViewReady)
                    ViewerWebView.Eval($"if(window.loadDataBase64) window.loadDataBase64('{b64}');");
                else
                    _pendingBase64Plan = b64;
            }
            else if (_isWebViewReady)
            {
                ViewerWebView.Eval("if(window.clearCanvas) window.clearCanvas();");
            }
        }
        catch { }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (Navigation.NavigationStack.Count > 1)
        {
            await Navigation.PopAsync();
            return;
        }
        if (Navigation.ModalStack.Count > 0)
        {
            await Navigation.PopModalAsync();
            return;
        }
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//DashboardPage");
        }
    }

    private void OnToggle3DClicked(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        _is3DMode = !_is3DMode;
        BtnToggle3D.Text = _is3DMode ? "2D" : "3D";
        ActualizarEncabezadoYPermisos();

        if (_is3DMode && UserSession.EsArquitecto)
        {
            ContextTitle.Text = "Vista 3D del Modelo";
            ContextDesc.Text = "Rota con un dedo, haz zoom con dos dedos. Toca 2D arriba para seguir editando.";
        }
        else if (!_is3DMode && UserSession.EsArquitecto)
        {
            OnModoLibreClicked(null, EventArgs.Empty);
        }

        ViewerWebView.Eval($"if(window.switchCameraMode) window.switchCameraMode({_is3DMode.ToString().ToLowerInvariant()});");
    }

    private void OnModoMoverClicked(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        ContextTitle.Text = "Mover, Seleccionar y Editar";
        ContextDesc.Text = "Arrastra un módulo para moverlo, arrastra el fondo para desplazar o toca un muro/espacio para editar o eliminar.";
        UpdateActiveButton(BtnModoMover);
        ViewerWebView.Eval("if(window.switchMode) window.switchMode('pan');");
    }

    private void OnModoLibreClicked(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        ContextTitle.Text = "Dibujo Libre Inteligente";
        ContextDesc.Text = "Traza con tu dedo sobre la cuadrícula para rectificar y generar muros automáticamente.";
        UpdateActiveButton(BtnModoLibre);
        ViewerWebView.Eval("if(window.switchMode) window.switchMode('freehand');");
    }

    private void OnModoMurosClicked(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        ContextTitle.Text = "Muros de Precisión";
        ContextDesc.Text = "Toca el punto inicial y el punto final del muro para ingresar su longitud exacta en metros.";
        UpdateActiveButton(BtnModoMuros);
        ViewerWebView.Eval("if(window.switchMode) window.switchMode('walls');");
    }

    private void OnToggleOrtoClicked(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        _isOrthoEnabled = !_isOrthoEnabled;
        bool isDark = Application.Current?.UserAppTheme == AppTheme.Dark;
        BtnOrto.BackgroundColor = _isOrthoEnabled
            ? Color.FromArgb("#0A4174")
            : (isDark ? Color.FromArgb("#1E1E22") : Color.FromArgb("#FFFFFF"));
        BtnOrto.Source = _isOrthoEnabled || isDark ? "ic_cad_ortho_white.svg" : "ic_cad_ortho.svg";
        ViewerWebView.Eval($"if(window.toggleOrtho) window.toggleOrtho({_isOrthoEnabled.ToString().ToLowerInvariant()});");
    }

    private void OnModoModulosClicked(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        ContextTitle.Text = "Módulos de Espacios";
        ContextDesc.Text = "Toca el lienzo para colocar un bloque de ambiente. Luego cámbialo de tamaño o nombre en modo Mover.";
        UpdateActiveButton(BtnModoModulos);
        ViewerWebView.Eval("if(window.setModuleType) window.setModuleType('room');");
    }

    private void OnPuertaClicked(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        ContextTitle.Text = "Colocar Puerta";
        ContextDesc.Text = "Toca sobre un muro para insertar y alinear automáticamente una puerta abatible 2D/3D.";
        UpdateActiveButton(BtnPuerta);
        ViewerWebView.Eval("if(window.setModuleType) window.setModuleType('door');");
    }

    private void OnVentanaClicked(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        ContextTitle.Text = "Colocar Ventana";
        ContextDesc.Text = "Toca sobre un muro para insertar y alinear automáticamente una ventana acristalada 2D/3D.";
        UpdateActiveButton(BtnVentana);
        ViewerWebView.Eval("if(window.setModuleType) window.setModuleType('window');");
    }

    private async void OnImportarDXFClicked(object? sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.iOS, new[] { "public.data", "public.content" } },
                { DevicePlatform.Android, new[] { "*/*" } },
                { DevicePlatform.WinUI, new[] { ".dxf" } },
                { DevicePlatform.MacCatalyst, new[] { "public.data" } }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona un plano CAD (.dxf)",
                FileTypes = customFileType
            });

            if (result == null) return;

            if (!result.FileName.EndsWith(".dxf", StringComparison.OrdinalIgnoreCase))
            {
                await AlertService.ShowAlertAsync("Formato Inválido", "Por favor selecciona un archivo con extensión .dxf");
                return;
            }

            using var stream = await result.OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            string b64 = Convert.ToBase64String(ms.ToArray());

            ViewerWebView.Eval($"if(window.processDXFBase64) window.processDXFBase64('{b64}');");
        }
        catch (Exception ex)
        {
            await AlertService.ShowAlertAsync("Error", "No se pudo importar el plano DXF: " + ex.Message);
        }
    }

    private void UpdateActiveButton(View activeBtn)
    {
        View[] tools = { BtnModoMover, BtnModoLibre, BtnModoMuros, BtnModoModulos, BtnPuerta, BtnVentana };
        foreach (var btn in tools)
        {
            btn.Opacity = 0.55;
            btn.Scale = 1.0;
        }
        activeBtn.Opacity = 1.0;
        activeBtn.Scale = 1.08;
    }

    private void OnUndoClicked(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        ViewerWebView.Eval("if(window.undoLastAction) window.undoLastAction();");
    }

    private async void OnHistorialClicked(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }
        if (UserSession.ActiveProject == null)
        {
            await AlertService.ShowAlertAsync("Aviso", "Selecciona primero un proyecto activo desde el Inicio.");
            return;
        }

        var versiones = await ApiService.GetVersionesByProyectoAsync(UserSession.ActiveProject.Idproyecto);
        if (versiones == null || versiones.Count == 0)
        {
            await AlertService.ShowAlertAsync("Historial", "Aún no hay versiones guardadas para este proyecto.");
            return;
        }

        var ordenadas = versiones.OrderByDescending(v => v.Numeroversion).ToList();
        var opciones = ordenadas
            .Select(v => $"Versión {v.Numeroversion}" + (v.Esactual == true ? " (Activa)" : $" - {v.FechaFormateada}"))
            .ToArray();

        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null) return;

        string? seleccion = await page.DisplayActionSheetAsync("Historial de Versiones", "Cancelar", null, opciones);
        if (string.IsNullOrEmpty(seleccion) || seleccion == "Cancelar") return;

        int idx = Array.IndexOf(opciones, seleccion);
        if (idx < 0 || idx >= ordenadas.Count) return;

        var elegida = ordenadas[idx];
        string? jsonPlano = elegida.Puntosreferencia;

        if (!ApiService.EsJsonPlanoValido(jsonPlano))
        {
            var espacio = await ApiService.GetEspacioFisicoByProyectoAsync(UserSession.ActiveProject.Idproyecto);
            jsonPlano = espacio?.Puntosreferencia;
        }

        if (ApiService.EsJsonPlanoValido(jsonPlano))
        {
            string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonPlano!));
            ViewerWebView.Eval($"if(window.loadDataBase64) window.loadDataBase64('{b64}');");

            if (UserSession.EsArquitecto && elegida.Esactual != true)
            {
                await ApiService.MarcarVersionComoActualAsync(elegida.Idversiondiseno, UserSession.ActiveProject.Idproyecto);
            }

            // CLIENTE: reflejar la versión que se está viendo
            if (!UserSession.EsArquitecto)
            {
                _versionTexto = $" · v{elegida.Numeroversion}";
                ActualizarSubtituloCliente();
            }

            await AlertService.ShowAlertAsync("Versión Cargada", $"Se cargó la Versión {elegida.Numeroversion} en el visor.", "OK");
        }
        else
        {
            await AlertService.ShowAlertAsync("Aviso", $"La Versión {elegida.Numeroversion} no contiene trazos guardados.");
        }
    }

    private async void OnSavePlanClicked(object? sender, EventArgs e)
    {
        if (!UserSession.EsArquitecto)
        {
            await AlertService.ShowAlertAsync("Acceso Restringido", "Solo el arquitecto puede guardar cambios en el plano.");
            return;
        }

        if (UserSession.ActiveProject == null)
        {
            await AlertService.ShowAlertAsync("Proyecto Requerido", "Selecciona un proyecto activo en Inicio antes de guardar el plano.");
            return;
        }

        try
        {
            string? result = await ViewerWebView.EvaluateJavaScriptAsync("window.getExportData()");
            if (string.IsNullOrWhiteSpace(result) || result == "null")
            {
                await AlertService.ShowAlertAsync("Error", "No se pudieron obtener los trazos del plano.");
                return;
            }

            string jsonData = DesempaquetarJsonWebView(result);
            int idProyecto = UserSession.ActiveProject.Idproyecto;

            var (ok, msg, nextVersion) = await ApiService.GuardarPlanoProyectoAsync(idProyecto, jsonData);

            if (ok)
            {
                await AlertService.ShowAlertAsync(
                    "Plano Guardado",
                    $"El diseño de '{UserSession.ActiveProject.Nombre}' (Versión {nextVersion}) se guardó correctamente.",
                    "OK");
            }
            else
            {
                await AlertService.ShowAlertAsync("Error", "No se pudo guardar el plano: " + msg);
            }
        }
        catch (Exception ex)
        {
            await AlertService.ShowAlertAsync("Error", "Excepción al guardar: " + ex.Message);
        }
    }

    private static string DesempaquetarJsonWebView(string raw)
    {
        string jsonData = raw.Trim();
        try
        {
            if (jsonData.StartsWith("\"") && jsonData.EndsWith("\""))
            {
                jsonData = JsonSerializer.Deserialize<string>(jsonData) ?? jsonData;
            }
            else if (jsonData.Contains("\\\""))
            {
                jsonData = jsonData.Replace("\\\"", "\"").Replace("\\\\", "\\");
            }
        }
        catch
        {
            jsonData = jsonData.Trim('"').Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
        return jsonData;
    }

    // =====================================================================
    // CLIENTE
    // =====================================================================

    private void ActualizarSubtituloCliente()
    {
        ClientSubtitleLabel.Text = _pickMode
            ? "Toca una zona del plano"
            : "Solo lectura" + _versionTexto;
    }

    private async Task ActualizarVersionClienteAsync()
    {
        _versionTexto = "";
        try
        {
            if (UserSession.ActiveProject != null)
            {
                var versiones = await ApiService.GetVersionesByProyectoAsync(UserSession.ActiveProject.Idproyecto);
                if (versiones != null && versiones.Count > 0)
                {
                    var actual = versiones.FirstOrDefault(v => v.Esactual == true)
                                 ?? versiones.OrderByDescending(v => v.Numeroversion).First();
                    _versionTexto = $" · v{actual.Numeroversion}";
                }
            }
        }
        catch { }
        ActualizarSubtituloCliente();
    }

    // La versión (subtítulo) abre el historial, así el cliente no pierde esa función
    private void OnClientSubtitleTapped(object? sender, EventArgs e)
    {
        if (_pickMode) return;
        OnHistorialClicked(null, EventArgs.Empty);
    }

    // ---- Desplegable de proyectos ----

    private async void OnProjectSelectorTapped(object? sender, EventArgs e)
    {
        if (ProjectDropdown.IsVisible)
        {
            CerrarDropdown();
            return;
        }

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        var proyectos = new List<ProyectoDto>();
        try
        {
            var vinculados = await ApiService.GetProyectosByClienteAsync(UserSession.Idusuario);
            if (vinculados != null) proyectos.AddRange(vinculados);
        }
        catch { }

        // El proyecto activo (el que ya venía del Inicio) siempre aparece en la lista
        if (UserSession.ActiveProject is ProyectoDto activoActual &&
            !proyectos.Any(p => p.Idproyecto == activoActual.Idproyecto))
        {
            proyectos.Insert(0, activoActual);
        }

        ProjectListHost.Children.Clear();

        if (proyectos.Count == 0)
        {
            var vacio = new Label { Text = "No tienes proyectos vinculados", FontSize = 13, Padding = new Thickness(12, 10) };
            vacio.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#5B6478"), Color.FromArgb("#94A3B8"));
            ProjectListHost.Children.Add(vacio);
        }
        else
        {
            foreach (var p in proyectos)
            {
                bool activo = p.Idproyecto == UserSession.ActiveProject?.Idproyecto;

                var nombre = new Label
                {
                    Text = p.Nombre,
                    FontSize = 15,
                    FontAttributes = activo ? FontAttributes.Bold : FontAttributes.None,
                    LineBreakMode = LineBreakMode.TailTruncation,
                    MaxLines = 1,
                    VerticalOptions = LayoutOptions.Center
                };
                nombre.SetAppThemeColor(Label.TextColorProperty, Color.FromArgb("#1F2A44"), Color.FromArgb("#FFFFFF"));

                var punto = new BoxView
                {
                    WidthRequest = 8,
                    HeightRequest = 8,
                    CornerRadius = 4,
                    Color = Color.FromArgb("#3B5FB5"),
                    VerticalOptions = LayoutOptions.Center,
                    IsVisible = activo
                };

                var fila = new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
                    Padding = new Thickness(12, 12),
                    ColumnSpacing = 10
                };
                fila.Add(nombre, 0, 0);
                fila.Add(punto, 1, 0);
                if (activo)
                    fila.SetAppThemeColor(VisualElement.BackgroundColorProperty, Color.FromArgb("#EEF2FD"), Color.FromArgb("#26304F"));

                var tap = new TapGestureRecognizer();
                var proyectoElegido = p;
                tap.Tapped += async (_, _) => await SeleccionarProyectoAsync(proyectoElegido);
                fila.GestureRecognizers.Add(tap);

                ProjectListHost.Children.Add(fila);
            }
        }

        ProjectDropdown.IsVisible = true;
        ClientBackdrop.IsVisible = true;
        ClientChevron.Rotation = 180;
    }
    private async Task SeleccionarProyectoAsync(ProyectoDto proyecto)
    {
        CerrarDropdown();
        CerrarPanelSolicitud();

        UserSession.ActiveProject = proyecto;
        ClientTitleLabel.Text = proyecto.Nombre;
        await CargarPlanoProyectoActivoAsync();
    }

    private void CerrarDropdown()
    {
        ProjectDropdown.IsVisible = false;
        ClientBackdrop.IsVisible = false;
        ClientChevron.Rotation = 0;
    }

    private void OnDropdownBackdropTapped(object? sender, EventArgs e) => CerrarDropdown();

    // ---- Solicitud de cambio ----

    private void OnClientSugerirClicked(object? sender, EventArgs e)
    {
        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        if (RequestPanel.IsVisible)
        {
            CerrarPanelSolicitud();
            return;
        }

        _zonaNombre = null; _zonaTipo = null; _zonaX = null; _zonaY = null;
        _pickMode = true;
        if (_isWebViewReady) ViewerWebView.Eval("if(window.setPickMode) window.setPickMode(true);");

        RequestZoneLabel.Text = "Sin zona seleccionada";
        MostrarPanelSolicitud();
    }

    private void AbrirPanelSolicitud(string? nombre, string? tipo, string? x, string? y)
    {
        if (_is3DMode) return;

        try { HapticFeedback.Default.Perform(HapticFeedbackType.Click); } catch { }

        _zonaNombre = string.IsNullOrWhiteSpace(nombre) ? "Zona del plano" : nombre;
        _zonaTipo = tipo;
        _zonaX = x;
        _zonaY = y;

        if (!_pickMode)
        {
            _pickMode = true;
            ViewerWebView.Eval("if(window.setPickMode) window.setPickMode(true);");
        }

        RequestZoneLabel.Text = _zonaNombre;
        MostrarPanelSolicitud();
    }

    private void MostrarPanelSolicitud()
    {
        CerrarDropdown();
        RequestPanel.IsVisible = true;
        BottomTabBar.IsVisible = false;
        ActualizarSubtituloCliente();
    }

    private void CerrarPanelSolicitud()
    {
        _pickMode = false;
        _zonaNombre = null; _zonaTipo = null; _zonaX = null; _zonaY = null;
        _fotoSolicitud = null;

        RequestEditor.Text = string.Empty;
        BtnRequestPhoto.SetAppThemeColor(VisualElement.BackgroundColorProperty, Color.FromArgb("#F0F2F7"), Color.FromArgb("#2C2C32"));
        RequestPanel.IsVisible = false;
        BottomTabBar.IsVisible = true;

        if (_isWebViewReady) ViewerWebView.Eval("if(window.setPickMode) window.setPickMode(false);");
        ActualizarSubtituloCliente();
    }

    private void OnRequestCloseClicked(object? sender, EventArgs e) => CerrarPanelSolicitud();

    private async void OnRequestPhotoClicked(object? sender, EventArgs e)
    {
        try
        {
            var foto = await MediaPicker.Default.PickPhotoAsync();
            if (foto == null) return;
            _fotoSolicitud = foto;
            BtnRequestPhoto.BackgroundColor = Color.FromArgb("#D3E0FA");
        }
        catch (Exception ex)
        {
            await AlertService.ShowAlertAsync("Error", "No se pudo adjuntar la foto: " + ex.Message);
        }
    }

    private async void OnRequestSendClicked(object? sender, EventArgs e)
    {
        if (_enviando) return;

        string texto = RequestEditor.Text?.Trim() ?? "";
        if (texto.Length == 0)
        {
            await AlertService.ShowAlertAsync("Solicitud", "Describe el cambio que necesitas antes de enviar.");
            return;
        }

        if (UserSession.ActiveProject == null)
        {
            await AlertService.ShowAlertAsync("Proyecto Requerido", "Selecciona un proyecto para enviar la solicitud.");
            return;
        }

        _enviando = true;
        BtnRequestSend.IsEnabled = false;

        try
        {
            // La zona viaja dentro de la descripción para que el arquitecto sepa dónde es el cambio
            string zona = _zonaNombre ?? "General";
            string descripcion = (_zonaX != null && _zonaY != null)
                ? $"[{zona} · x:{_zonaX} m, y:{_zonaY} m] {texto}"
                : $"[{zona}] {texto}";

            // Título corto (el mismo largo máximo que usaba el flujo anterior)
            string prefijo = $"[{zona}] ";
            int max = Math.Max(10, 50 - prefijo.Length);
            string resumen = texto.Length > max ? texto[..max] + "…" : texto;

            var request = new CrearSugerenciaRequest
            {
                Idproyecto = UserSession.ActiveProject.Idproyecto,
                Idusuario = UserSession.Idusuario,
                Titulo = prefijo + resumen,
                Descripcion = descripcion
            };

            var (ok, msg, _) = await ApiService.CrearSugerenciaAsync(request);

            if (ok)
            {
                CerrarPanelSolicitud();
                await AlertService.ShowAlertAsync("Solicitud enviada", "El arquitecto recibirá tu solicitud.", "OK");
            }
            else
            {
                await AlertService.ShowAlertAsync("Error", "No se pudo enviar la solicitud: " + msg);
            }
        }
        catch (Exception ex)
        {
            await AlertService.ShowAlertAsync("Error", "Excepción al enviar: " + ex.Message);
        }
        finally
        {
            _enviando = false;
            BtnRequestSend.IsEnabled = true;
        }
    }
}