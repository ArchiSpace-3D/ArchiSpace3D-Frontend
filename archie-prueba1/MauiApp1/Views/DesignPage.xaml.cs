using MauiApp1.Models;
using MauiApp1.Services;
using System.Text;
using System.Text.Json;

namespace MauiApp1.Views;

public partial class DesignPage : ContentPage
{
    private bool _is3DMode = false;
    private bool _isOrthoEnabled = false;
    private bool _isWebViewReady = false;
    private int? _loadedProjectId = null;
    private string? _pendingBase64Plan = null;

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

        if (projId == null)
        {
            if (_isWebViewReady)
                ViewerWebView.Eval("if(window.clearCanvas) window.clearCanvas();");
            return;
        }

        try
        {
            string? planJson = null;

            // 1. Intentar desde la versión activa (o más reciente con datos)
            var versiones = await ApiService.GetVersionesByProyectoAsync(projId.Value);
            if (versiones != null && versiones.Count > 0)
            {
                var candidata = versiones
                    .OrderByDescending(v => v.Esactual == true)
                    .ThenByDescending(v => v.Numeroversion)
                    .FirstOrDefault(v => EsJsonPlanoValido(v.Puntosreferencia));

                if (candidata != null)
                    planJson = candidata.Puntosreferencia;
            }

            // 2. Fallback a EspacioFisico del proyecto
            if (!EsJsonPlanoValido(planJson))
            {
                var espacio = await ApiService.GetEspacioFisicoByProyectoAsync(projId.Value);
                if (espacio != null && EsJsonPlanoValido(espacio.Puntosreferencia))
                {
                    planJson = espacio.Puntosreferencia;
                }
            }

            if (EsJsonPlanoValido(planJson))
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

    private static bool EsJsonPlanoValido(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return false;
        var trimmed = raw.Trim();
        return trimmed.StartsWith("{") && trimmed != "{}" && (trimmed.Contains("points") || trimmed.Contains("modules"));
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

        if (!EsJsonPlanoValido(jsonPlano))
        {
            var espacio = await ApiService.GetEspacioFisicoByProyectoAsync(UserSession.ActiveProject.Idproyecto);
            jsonPlano = espacio?.Puntosreferencia;
        }

        if (EsJsonPlanoValido(jsonPlano))
        {
            string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonPlano!));
            ViewerWebView.Eval($"if(window.loadDataBase64) window.loadDataBase64('{b64}');");

            if (UserSession.EsArquitecto && elegida.Esactual != true)
            {
                await ApiService.MarcarVersionComoActualAsync(elegida.Idversiondiseno, UserSession.ActiveProject.Idproyecto);
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

            // 1. Crear nueva versión de diseño
            var versiones = await ApiService.GetVersionesByProyectoAsync(idProyecto);
            int nextVersion = (versiones != null && versiones.Count > 0)
                ? versiones.Max(v => v.Numeroversion) + 1
                : 1;

            var nuevaVersion = new CrearVersionDisenoRequest
            {
                Idproyecto = idProyecto,
                Numeroversion = nextVersion,
                Esactual = true,
                Fechacreacion = DateTime.UtcNow,
                Puntosreferencia = jsonData
            };
            var (verOk, _, _) = await ApiService.GuardarVersionAsync(nuevaVersion);

            // 2. Guardar respaldo sincronizado en EspacioFisico (__PLANO_CAD__)
            var existente = await ApiService.GetEspacioFisicoByProyectoAsync(idProyecto);
            bool espOk;
            string espMsg;

            if (existente != null && (existente.Descripcion == "__PLANO_CAD__" || string.IsNullOrWhiteSpace(existente.Descripcion)))
            {
                existente.Descripcion = "__PLANO_CAD__";
                existente.Puntosreferencia = jsonData;
                (espOk, espMsg) = await ApiService.ActualizarEspacioFisicoAsync(existente.Idespaciofisico, existente);
            }
            else
            {
                var req = new CrearEspacioFisicoRequest
                {
                    Idproyecto = idProyecto,
                    Descripcion = "__PLANO_CAD__",
                    Puntosreferencia = jsonData
                };
                var (okCrear, msgCrear, _) = await ApiService.GuardarEspacioFisicoAsync(req);
                if (!okCrear && existente != null)
                {
                    // Fallback si el servidor remoto aún tiene restricción 1-a-1 en espaciofisico
                    existente.Puntosreferencia = jsonData;
                    (espOk, espMsg) = await ApiService.ActualizarEspacioFisicoAsync(existente.Idespaciofisico, existente);
                }
                else
                {
                    espOk = okCrear;
                    espMsg = msgCrear;
                }
            }

            if (verOk || espOk)
            {
                await AlertService.ShowAlertAsync(
                    "Plano Guardado",
                    $"El diseño de '{UserSession.ActiveProject.Nombre}' (Versión {nextVersion}) se guardó correctamente.",
                    "OK");
            }
            else
            {
                await AlertService.ShowAlertAsync("Error", "No se pudo guardar el plano: " + espMsg);
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
}
