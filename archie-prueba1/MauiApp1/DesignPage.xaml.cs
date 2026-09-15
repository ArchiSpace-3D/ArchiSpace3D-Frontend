using MauiApp1.Models;
using MauiApp1.Services;
using System.Text.Json;
using System.Globalization;

namespace MauiApp1;

public partial class DesignPage : ContentPage
{
    private EspacioFisicoDto? _espacioActual;
    private ProyectoDto? _proyectoActual;
    private int _idVersionDisenoActiva = 0;

    public DesignPage()
    {
        InitializeComponent();
#if ANDROID
        Viewer3D.Source = new UrlWebViewSource { Url = "file:///android_asset/viewer3d.html" };
#elif IOS
        Viewer3D.Source = new UrlWebViewSource { Url = Foundation.NSBundle.MainBundle.BundlePath + "/viewer3d.html" };
#else
        Viewer3D.Source = new UrlWebViewSource { Url = "viewer3d.html" };
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Ejecutar animación sin importar si hay proyecto o no
        _ = Task.WhenAll(
            MainScroll.FadeTo(1, 600, Easing.CubicOut),
            MainScroll.TranslateTo(0, 0, 600, Easing.CubicOut)
        );

        _proyectoActual = UserSession.ActiveProject;
        if (_proyectoActual == null)
        {
            LblProyectoDesign.Text = "Ninguno";
            return;
        }

        LblProyectoDesign.Text = _proyectoActual.Nombre;
        CargarDatosEspacio();
        _ = CargarCatalogoAsync();
    }

    private async void CargarDatosEspacio()
    {
        if (_proyectoActual == null) return;
        
        var espacio = await ApiService.GetEspacioFisicoByProyectoAsync(_proyectoActual.Idproyecto);
        if (espacio != null)
        {
            _espacioActual = espacio;
            AltoEntry.Text = _espacioActual.Altoaproximado?.ToString(CultureInfo.InvariantCulture) ?? "0";
            AnchoEntry.Text = _espacioActual.Anchoaproximado?.ToString(CultureInfo.InvariantCulture) ?? "0";
            LargoEntry.Text = _espacioActual.Largoaproximado?.ToString(CultureInfo.InvariantCulture) ?? "0";

            if (_espacioActual.Anchoaproximado > 0 && _espacioActual.Largoaproximado > 0)
            {
                string anchoStr = _espacioActual.Anchoaproximado.Value.ToString(CultureInfo.InvariantCulture);
                string largoStr = _espacioActual.Largoaproximado.Value.ToString(CultureInfo.InvariantCulture);
                string altoStr = (_espacioActual.Altoaproximado ?? 2.5m).ToString(CultureInfo.InvariantCulture);
                await Viewer3D.EvaluateJavaScriptAsync($"createRoom({anchoStr}, {largoStr}, {altoStr});");
            }
        }
        else
        {
            _espacioActual = null;
            AltoEntry.Text = "0";
            AnchoEntry.Text = "0";
            LargoEntry.Text = "0";
        }

        // Buscar versión de diseño para los elementos estructurales
        var versiones = await ApiService.GetVersionesByProyectoAsync(_proyectoActual.Idproyecto);
        if (versiones != null && versiones.Count > 0)
        {
            _idVersionDisenoActiva = versiones[0].Idversiondiseno;
            await CargarElementosEstructurales(_idVersionDisenoActiva);
        }
        else
        {
            var reqVersion = new CrearVersionDisenoRequest
            {
                Idproyecto = _proyectoActual.Idproyecto,
                Numeroversion = 1,
                Esactual = true,
                Fechacreacion = DateTime.UtcNow
            };
            var versionResult = await ApiService.CrearVersionDisenoAsync(reqVersion);
            if (versionResult.Success)
            {
                var vers = await ApiService.GetVersionesByProyectoAsync(_proyectoActual.Idproyecto);
                if (vers != null && vers.Count > 0)
                {
                    _idVersionDisenoActiva = vers[0].Idversiondiseno;
                    await CargarElementosEstructurales(_idVersionDisenoActiva);
                }
            }
            else
            {
                ElementosList.Children.Clear();
            }
        }
    }

    private async Task CargarElementosEstructurales(int idVersion)
    {
        ElementosList.Children.Clear();
        var elementos = await ApiService.GetElementosByVersionAsync(idVersion);
        if (elementos == null || elementos.Count == 0) return;

        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var cardBg = isDark ? Color.FromArgb("#000000") : Color.FromArgb("#FFFFFF");
        var textColor = isDark ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#001D39");
        var subTextColor = isDark ? Color.FromArgb("#7BBDE8") : Color.FromArgb("#6EA2B3");
        var deleteBg = isDark ? Color.FromArgb("#3D1616") : Color.FromArgb("#FFF0F0");

        foreach (var elem in elementos)
        {
            var btnDelete = new Border
            {
                BackgroundColor = deleteBg,
                StrokeThickness = 0,
                WidthRequest = 32,
                HeightRequest = 32,
                VerticalOptions = LayoutOptions.Center,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(16) }
            };
            btnDelete.Content = new Image { Source = "ic_close.svg", WidthRequest = 14, HeightRequest = 14, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            
            var tapDelete = new TapGestureRecognizer { CommandParameter = elem.Idelementoestructural };
            tapDelete.Tapped += async (s, e) =>
            {
                var confirm = await Application.Current!.Windows[0].Page!.DisplayAlertAsync("Borrar", $"¿Eliminar {elem.Tipo}?", "Sí", "No");
                if (confirm)
                {
                    await ApiService.EliminarElementoEstructuralAsync(elem.Idelementoestructural);
                    await CargarElementosEstructurales(_idVersionDisenoActiva);
                }
            };
            btnDelete.GestureRecognizers.Add(tapDelete);

            var infoStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            infoStack.Children.Add(new Label { Text = elem.Tipo, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = textColor });
            infoStack.Children.Add(new Label { Text = $"Alto: {elem.Dimensionalto} - Ancho: {elem.Dimensionancho}", FontSize = 12, TextColor = subTextColor });

            var row = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } }, Padding = new Thickness(15), ColumnSpacing = 10 };
            row.Children.Add(infoStack);
            Grid.SetColumn(btnDelete, 1);
            row.Children.Add(btnDelete);

            var card = new Border
            {
                BackgroundColor = cardBg,
                StrokeThickness = 0,
                Margin = new Thickness(0, 0, 0, 10),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) },
                Content = row
            };
            card.Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.05f, Offset = new Point(0,2), Radius = 5 };
            ElementosList.Children.Add(card);
        }
    }

    private decimal ParseDecimal(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return 0;
        val = val.Replace(",", ".");
        if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec)) return dec;
        return 0;
    }

    private async void OnGuardarFisicoClicked(object sender, EventArgs e)
    {
        if (UserSession.Rol == "Cliente")
        {
            await ShowAlertAsync("Solo Lectura", "Los clientes no pueden modificar el diseño.", "OK");
            return;
        }
        if (_proyectoActual == null)
        {
            await ShowAlertAsync("Aviso", "Selecciona un proyecto primero en Inicio.", "OK");
            return;
        }

        decimal w = ParseDecimal(AnchoEntry.Text);
        decimal l = ParseDecimal(LargoEntry.Text);
        decimal h = ParseDecimal(AltoEntry.Text);

        if (w <= 0 || l <= 0 || h <= 0)
        {
            await ShowAlertAsync("Datos Inválidos", "Por favor, ingresa medidas mayores a cero para el ancho, largo y alto.", "OK");
            return;
        }

        if (_espacioActual == null)
        {
            var req = new CrearEspacioFisicoRequest
            {
                Idproyecto = _proyectoActual.Idproyecto,
                Altoaproximado = h,
                Anchoaproximado = w,
                Largoaproximado = l,
                Descripcion = "Espacio Principal"
            };
            var res = await ApiService.GuardarEspacioFisicoAsync(req);
            if (res.Success)
            {
                CargarDatosEspacio();
                await ShowAlertAsync("Éxito", "Espacio guardado", "OK");
            }
        }
        else
        {
            _espacioActual.Altoaproximado = h;
            _espacioActual.Anchoaproximado = w;
            _espacioActual.Largoaproximado = l;
            var res = await ApiService.ActualizarEspacioFisicoAsync(_espacioActual.Idespaciofisico, _espacioActual);
            if (res.Success) await ShowAlertAsync("Éxito", "Espacio actualizado", "OK");
        }
    }

    private async void OnAddElementoClicked(object sender, EventArgs e)
    {
        if (UserSession.Rol == "Cliente")
        {
            await ShowAlertAsync("Solo Lectura", "Los clientes no pueden modificar el diseño.", "OK");
            return;
        }
        if (_espacioActual == null || (_espacioActual.Anchoaproximado == 0 && _espacioActual.Largoaproximado == 0))
        {
            await ShowAlertAsync("Aviso", "Debes guardar el perímetro de la planta (Paso 1) antes de agregar estructuras.", "OK");
            return;
        }

        if (_idVersionDisenoActiva == 0)
        {
            await ShowAlertAsync("Aviso", "Asegúrate de que haya una versión de diseño activa.", "OK");
            return;
        }
        ElementBackdrop.IsVisible = true;
        await ElementBackdrop.FadeToAsync(1, 200);
        await ElementSheetModal.TranslateToAsync(0, 0, 350, Easing.CubicOut);
        ElementSheetModal.IsVisible = true;
    }

    private async Task CargarCatalogoAsync()
    {
        LoadingCatalogIndicator.IsVisible = true;
        LoadingCatalogIndicator.IsRunning = true;
        CatalogCollectionView.ItemsSource = null;

        var catalog = await ApiService.GetModelosImportadosAsync();
        
        LoadingCatalogIndicator.IsRunning = false;
        LoadingCatalogIndicator.IsVisible = false;
        
        CatalogCollectionView.ItemsSource = catalog;
    }

    private async void OnRefreshCatalogClicked(object sender, EventArgs e)
    {
        await CargarCatalogoAsync();
    }

    private async void OnAddModeloToDesignClicked(object sender, EventArgs e)
    {
        if (UserSession.Rol == "Cliente")
        {
            await ShowAlertAsync("Solo Lectura", "Los clientes no pueden modificar el diseño.", "OK");
            return;
        }
        if (_espacioActual == null || (_espacioActual.Anchoaproximado == 0 && _espacioActual.Largoaproximado == 0))
        {
            await ShowAlertAsync("Aviso", "Debes guardar el perímetro de la planta (Paso 1) antes de agregar mobiliario.", "OK");
            return;
        }

        if (_idVersionDisenoActiva == 0)
        {
            await ShowAlertAsync("Aviso", "No hay una versión de diseño activa para guardar el modelo.", "OK");
            return;
        }

        if (sender is Button btn && btn.CommandParameter is MauiApp1.Models.ModeloImportadoDto modelo)
        {
            // Inject in 3D viewer
            if (!string.IsNullOrEmpty(modelo.Rutastorage))
            {
                await Viewer3D.EvaluateJavaScriptAsync($"loadModel('{modelo.Rutastorage}');");
            }
            
            // En un flujo real, aquí actualizaríamos transformaciones (posición, rotación)
            await ShowAlertAsync("Añadido", $"El modelo '{modelo.Nombrearchivo}' se ha añadido al diseño actual.", "OK");
            // Aquí iría un llamado a ApiService.CrearModeloImportadoAsync o ActualizarTransformModeloAsync
        }
    }

    private async void OnCloseElementSheetClicked(object sender, EventArgs e)
    {
        await CloseElementSheet();
    }

    private async Task CloseElementSheet()
    {
        await ElementSheetModal.TranslateToAsync(0, 600, 250, Easing.CubicIn);
        await ElementBackdrop.FadeToAsync(0, 200);
        ElementBackdrop.IsVisible = false;
        ElementSheetModal.IsVisible = false;
    }

    private async void OnSubmitElementoClicked(object sender, EventArgs e)
    {
        if (UserSession.Rol == "Cliente") return;
        if (_idVersionDisenoActiva == 0) return;
        var req = new CrearElementoEstructuralRequest
        {
            Idversiondiseno = _idVersionDisenoActiva,
            Tipo = TipoElementoPicker.SelectedItem?.ToString() ?? "Pilar",
            Dimensionalto = ParseDecimal(AltoElementoEntry.Text),
            Dimensionancho = ParseDecimal(AnchoElementoEntry.Text),
            Dimensionprofundidad = 0
        };
        var res = await ApiService.CrearElementoEstructuralAsync(req);
        if (res.Success)
        {
            await CloseElementSheet();
            await CargarElementosEstructurales(_idVersionDisenoActiva);
        }
    }

    private async Task ShowAlertAsync(string title, string message, string cancel)
    {
        if (Application.Current?.Windows.Count > 0)
        {
            await Application.Current.Windows[0].Page!.DisplayAlertAsync(title, message, cancel);
        }
    }

    private async void OnVerVersionesClicked(object sender, EventArgs e)
    {
        if (UserSession.ActiveProject == null) return;

        VersionsBackdrop.IsVisible = true;
        await VersionsBackdrop.FadeToAsync(1, 200);
        VersionsSheetModal.IsVisible = true;
        await VersionsSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut);

        await CargarVersionesAsync();
    }

    private async Task CargarVersionesAsync()
    {
        if (UserSession.ActiveProject == null) return;
        
        LoadingVersionsIndicator.IsVisible = true;
        LoadingVersionsIndicator.IsRunning = true;
        VersionsCollectionView.ItemsSource = null;

        var versiones = await ApiService.GetVersionesByProyectoAsync(UserSession.ActiveProject.Idproyecto);
        
        LoadingVersionsIndicator.IsRunning = false;
        LoadingVersionsIndicator.IsVisible = false;
        
        VersionsCollectionView.ItemsSource = versiones;
    }

    private async void OnCloseVersionsSheetClicked(object sender, EventArgs e)
    {
        await VersionsSheetModal.TranslateToAsync(0, 600, 250, Easing.CubicIn);
        await VersionsBackdrop.FadeToAsync(0, 200);
        VersionsBackdrop.IsVisible = false;
        VersionsSheetModal.IsVisible = false;
    }

    private async void OnGuardarNuevaVersionClicked(object sender, EventArgs e)
    {
        if (UserSession.Rol == "Cliente")
        {
            await ShowAlertAsync("Solo Lectura", "Los clientes no pueden modificar el diseño.", "OK");
            return;
        }
        if (UserSession.ActiveProject == null) return;
        
        var versiones = (List<MauiApp1.Models.VersionDisenoDto>)(VersionsCollectionView.ItemsSource ?? new List<MauiApp1.Models.VersionDisenoDto>());
        int nextVersion = versiones.Count > 0 ? versiones.Max(v => v.Numeroversion) + 1 : 1;

        var req = new MauiApp1.Models.CrearVersionDisenoRequest
        {
            Idproyecto = UserSession.ActiveProject.Idproyecto,
            Numeroversion = nextVersion,
            Esactual = true
        };

        var (success, msg, data) = await ApiService.CrearVersionDisenoAsync(req);
        if (success)
        {
            await ShowAlertAsync("Éxito", "Nueva versión guardada correctamente.", "OK");
            await CargarVersionesAsync();
        }
        else
        {
            await ShowAlertAsync("Error", $"No se pudo guardar la versión: {msg}", "OK");
        }
    }

    private async void OnCargarVersionClicked(object sender, EventArgs e)
    {
        if (UserSession.Rol == "Cliente")
        {
            await ShowAlertAsync("Solo Lectura", "Los clientes no pueden modificar el diseño.", "OK");
            return;
        }
        if (sender is Button btn && btn.CommandParameter is MauiApp1.Models.VersionDisenoDto version && UserSession.ActiveProject != null)
        {
            var (success, msg) = await ApiService.MarcarVersionComoActualAsync(version.Idversiondiseno, UserSession.ActiveProject.Idproyecto);
            if (success)
            {
                await ShowAlertAsync("Cargado", $"Se ha cargado la versión #{version.Numeroversion}", "OK");
                OnCloseVersionsSheetClicked(this, EventArgs.Empty);
                // Here we would reload the canvas items for this version
            }
            else
            {
                await ShowAlertAsync("Error", $"Error al cargar versión: {msg}", "OK");
            }
        }
    }
}
