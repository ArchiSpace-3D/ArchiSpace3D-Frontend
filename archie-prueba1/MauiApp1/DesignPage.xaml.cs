using System.Collections.ObjectModel;
using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1;

public partial class DesignPage : ContentPage
{
    private readonly ObservableCollection<VersionDisenoDto> _versiones = new ObservableCollection<VersionDisenoDto>();
    private VersionDisenoDto? _versionSeleccionada;
    private EspacioFisicoDto? _espacioActual;

    public DesignPage()
    {
        InitializeComponent();
        VersionesCollectionView.ItemsSource = _versiones;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarDatosProyectoAsync();
    }

    private async Task CargarDatosProyectoAsync()
    {
        if (UserSession.ActiveProject == null)
        {
            ProjectHeaderLabel.Text = "Sin proyecto activo. Selecciona uno en Inicio.";
            return;
        }

        ProjectHeaderLabel.Text = $"Proyecto: {UserSession.ActiveProject.Nombre}";

        bool esArquitecto = UserSession.Rol == "Arquitecto";
        BtnCrearVersion.IsVisible = esArquitecto;
        BtnAgregarElemento.IsVisible = esArquitecto;

        await Task.WhenAll(
            CargarEspacioFisicoAsync(),
            CargarVersionesAsync()
        );
    }

    private async void OnRefreshClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn)
        {
            _ = btn.RotateToAsync(360, 400).ContinueWith(_ => btn.Rotation = 0);
        }
        await CargarDatosProyectoAsync();
        await ShowToastAsync("Datos actualizados");
    }

    // ==================== ESPACIO FISICO ====================

    private async Task CargarEspacioFisicoAsync()
    {
        if (UserSession.ActiveProject == null) return;

        _espacioActual = await ApiService.GetEspacioFisicoByProyectoAsync(UserSession.ActiveProject.Idproyecto);

        if (_espacioActual != null)
        {
            LblPlanta.Text = $"{_espacioActual.Anchoaproximado ?? 0:0.##} x {_espacioActual.Largoaproximado ?? 0:0.##} m";
            LblAlto.Text = $"{_espacioActual.Altoaproximado ?? 0:0.##} m";
            LblAreaVol.Text = $"{_espacioActual.AreaCalculada:0.##} m²\n({_espacioActual.VolumenCalculado:0.##} m³)";
            LblAzimut.Text = $"Orientación Azimut: {_espacioActual.Orientacionazimuth ?? 0:0}° ({_espacioActual.Descripcion ?? "Espacio Principal"})";
        }
        else
        {
            LblPlanta.Text = "-- x -- m";
            LblAlto.Text = "-- m";
            LblAreaVol.Text = "-- m²";
            LblAzimut.Text = "Orientación Azimut: No configurado";
        }
    }

    private async void OnOpenEditEspacioSheetClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.94, 60); await btn.ScaleToAsync(1.0, 60); }
        if (UserSession.ActiveProject == null)
        {
            await ShowToastAsync("Selecciona un proyecto primero.");
            return;
        }

        if (_espacioActual != null)
        {
            EntryDescripcionEspacio.Text = _espacioActual.Descripcion;
            EntryAnchoEspacio.Text = _espacioActual.Anchoaproximado?.ToString();
            EntryLargoEspacio.Text = _espacioActual.Largoaproximado?.ToString();
            EntryAltoEspacio.Text = _espacioActual.Altoaproximado?.ToString();
            EntryAzimutEspacio.Text = _espacioActual.Orientacionazimuth?.ToString();
        }

        EspacioSheetModal.IsVisible = true;
        EspacioBackdrop.Opacity = 0;
        EspacioSheetCard.TranslationY = 400;

        _ = EspacioBackdrop.FadeToAsync(1.0, 250);
        await EspacioSheetCard.TranslateToAsync(0, 0, 300, Easing.CubicOut);
    }

    private async void OnCloseEspacioSheetClicked(object? sender, EventArgs e)
    {
        _ = EspacioBackdrop.FadeToAsync(0, 200);
        await EspacioSheetCard.TranslateToAsync(0, 400, 250, Easing.CubicIn);
        EspacioSheetModal.IsVisible = false;
    }

    private async void OnGuardarEspacioClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
        if (UserSession.ActiveProject == null) return;

        decimal.TryParse(EntryAnchoEspacio.Text, out decimal ancho);
        decimal.TryParse(EntryLargoEspacio.Text, out decimal largo);
        decimal.TryParse(EntryAltoEspacio.Text, out decimal alto);
        decimal.TryParse(EntryAzimutEspacio.Text, out decimal azimut);

        var request = new CrearEspacioFisicoRequest
        {
            Idproyecto = UserSession.ActiveProject.Idproyecto,
            Descripcion = EntryDescripcionEspacio.Text?.Trim() ?? "Área Principal",
            Anchoaproximado = ancho > 0 ? ancho : null,
            Largoaproximado = largo > 0 ? largo : null,
            Altoaproximado = alto > 0 ? alto : null,
            Orientacionazimuth = azimut >= 0 ? azimut : null
        };

        var (success, message, data) = await ApiService.GuardarEspacioFisicoAsync(request);
        OnCloseEspacioSheetClicked(null, EventArgs.Empty);

        if (success && data != null)
        {
            _espacioActual = data;
            await CargarEspacioFisicoAsync();
            await ShowToastAsync("Espacio físico guardado.");
        }
        else
        {
            await ShowToastAsync(message);
        }
    }

    // ==================== VERSIONES DE DISENO ====================

    private async Task CargarVersionesAsync()
    {
        if (UserSession.ActiveProject == null) return;

        _versiones.Clear();
        var versiones = await ApiService.GetVersionesByProyectoAsync(UserSession.ActiveProject.Idproyecto);

        foreach (var v in versiones)
        {
            _versiones.Add(v);
        }

        _versionSeleccionada = _versiones.FirstOrDefault(v => v.Esactual == true) ?? _versiones.FirstOrDefault();

        if (_versionSeleccionada != null)
        {
            SubtituloElementosLabel.Text = $"Componentes de la {_versionSeleccionada.TituloVersion}";
            await Task.WhenAll(
                CargarElementosEstructuralesAsync(_versionSeleccionada.Idversiondiseno),
                CargarModelosImportadosAsync(_versionSeleccionada.Idversiondiseno)
            );
        }
        else
        {
            ElementosStackLayout.Children.Clear();
            ModelosStackLayout.Children.Clear();
            SubtituloElementosLabel.Text = "Crea una versión para asociar elementos 3D.";
        }
    }

    private async void OnCrearVersionClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.94, 60); await btn.ScaleToAsync(1.0, 60); }
        if (UserSession.ActiveProject == null) return;

        int proximaVersion = _versiones.Count + 1;
        var nueva = new CrearVersionDisenoRequest
        {
            Idproyecto = UserSession.ActiveProject.Idproyecto,
            Numeroversion = proximaVersion,
            Esactual = true
        };

        var (success, message, creada) = await ApiService.CrearVersionDisenoAsync(nueva);
        if (success && creada != null)
        {
            await CargarVersionesAsync();
            await ShowToastAsync($"¡Versión {creada.Numeroversion} creada y activada!");
        }
        else
        {
            await ShowToastAsync(message);
        }
    }

    private async void OnVersionSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not VersionDisenoDto seleccionada) return;

        _versionSeleccionada = seleccionada;

        if (UserSession.Rol == "Arquitecto" && seleccionada.Esactual != true && UserSession.ActiveProject != null)
        {
            await ApiService.MarcarVersionComoActualAsync(seleccionada.Idversiondiseno, UserSession.ActiveProject.Idproyecto);
            await CargarVersionesAsync();
            await ShowToastAsync($"Versión {seleccionada.Numeroversion} marcada como actual");
            return;
        }

        SubtituloElementosLabel.Text = $"Componentes de la {seleccionada.TituloVersion}";
        await Task.WhenAll(
            CargarElementosEstructuralesAsync(seleccionada.Idversiondiseno),
            CargarModelosImportadosAsync(seleccionada.Idversiondiseno)
        );
    }

    // ==================== ELEMENTOS ESTRUCTURALES ====================

    private async Task CargarElementosEstructuralesAsync(int idVersion)
    {
        ElementosStackLayout.Children.Clear();
        var elementos = await ApiService.GetElementosByVersionAsync(idVersion);

        if (elementos == null || elementos.Count == 0)
        {
            ElementosStackLayout.Children.Add(new Label
            {
                Text = "No hay elementos estructurales registrados en esta versión.",
                FontSize = 12,
                TextColor = Color.FromArgb("#7A8485"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 8)
            });
            return;
        }

        foreach (var elem in elementos)
        {
            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#F5F3EF"),
                Stroke = Color.FromArgb("#DCD7C9"),
                StrokeThickness = 1,
                Padding = new Thickness(14, 10),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) }
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Star },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var infoStack = new VerticalStackLayout { Spacing = 2 };
            infoStack.Children.Add(new Label { Text = $"{elem.Tipo} ({elem.Material ?? "Sin material"})", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2C3639") });
            infoStack.Children.Add(new Label { Text = $"Cotas: {elem.DimensionesTexto}", FontSize = 12, TextColor = Color.FromArgb("#7A8485") });

            grid.Children.Add(infoStack);

            if (UserSession.Rol == "Arquitecto")
            {
                var delBtn = new Image
                {
                    Source = "ic_trash.svg",
                    WidthRequest = 18,
                    HeightRequest = 18,
                    VerticalOptions = LayoutOptions.Center
                };
                int idElem = elem.Idelementoestructural;
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (s, e) =>
                {
                    await ApiService.EliminarElementoEstructuralAsync(idElem);
                    await CargarElementosEstructuralesAsync(idVersion);
                    await ShowToastAsync("Elemento eliminado");
                };
                delBtn.GestureRecognizers.Add(tap);
                Grid.SetColumn(delBtn, 1);
                grid.Children.Add(delBtn);
            }

            card.Content = grid;
            ElementosStackLayout.Children.Add(card);
        }
    }

    private async void OnOpenAgregarElementoSheetClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.94, 60); await btn.ScaleToAsync(1.0, 60); }
        if (_versionSeleccionada == null)
        {
            await ShowToastAsync("Primero crea una versión de diseño.");
            return;
        }

        ElementoSheetModal.IsVisible = true;
        ElementoBackdrop.Opacity = 0;
        ElementoSheetCard.TranslationY = 400;

        _ = ElementoBackdrop.FadeToAsync(1.0, 250);
        await ElementoSheetCard.TranslateToAsync(0, 0, 300, Easing.CubicOut);
    }

    private async void OnCloseElementoSheetClicked(object? sender, EventArgs e)
    {
        _ = ElementoBackdrop.FadeToAsync(0, 200);
        await ElementoSheetCard.TranslateToAsync(0, 400, 250, Easing.CubicIn);
        ElementoSheetModal.IsVisible = false;
    }

    private async void OnGuardarElementoClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
        if (_versionSeleccionada == null) return;

        string tipo = PickerTipoElemento.SelectedItem?.ToString() ?? "Muro";
        decimal.TryParse(EntryAnchoElem.Text, out decimal ancho);
        decimal.TryParse(EntryAltoElem.Text, out decimal alto);
        decimal.TryParse(EntryProfElem.Text, out decimal prof);

        var nuevo = new CrearElementoEstructuralRequest
        {
            Idversiondiseno = _versionSeleccionada.Idversiondiseno,
            Tipo = tipo,
            Material = EntryMaterialElemento.Text?.Trim() ?? "Concreto",
            Dimensionancho = ancho > 0 ? ancho : 1,
            Dimensionalto = alto > 0 ? alto : 2.5m,
            Dimensionprofundidad = prof > 0 ? prof : 0.2m
        };

        var (success, message, _) = await ApiService.CrearElementoEstructuralAsync(nuevo);
        OnCloseElementoSheetClicked(null, EventArgs.Empty);

        if (success)
        {
            await CargarElementosEstructuralesAsync(_versionSeleccionada.Idversiondiseno);
            await ShowToastAsync("Elemento agregado con éxito.");
        }
        else
        {
            await ShowToastAsync(message);
        }
    }

    // ==================== MODELOS IMPORTADOS ====================

    private async Task CargarModelosImportadosAsync(int idVersion)
    {
        ModelosStackLayout.Children.Clear();
        var modelos = await ApiService.GetModelosByVersionAsync(idVersion);

        if (modelos == null || modelos.Count == 0)
        {
            ModelosStackLayout.Children.Add(new Label
            {
                Text = "No hay archivos 3D importados en esta versión.",
                FontSize = 12,
                TextColor = Color.FromArgb("#7A8485"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 4)
            });
            return;
        }

        foreach (var mod in modelos)
        {
            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#F5F3EF"),
                Stroke = Color.FromArgb("#DCD7C9"),
                StrokeThickness = 1,
                Padding = new Thickness(14, 10),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) }
            };

            var stack = new VerticalStackLayout { Spacing = 2 };
            stack.Children.Add(new Label { Text = mod.Nombrearchivo, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2C3639") });
            stack.Children.Add(new Label { Text = $"Formato: {mod.Formato.ToUpper()} • Almacenamiento: {mod.Rutastorage}", FontSize = 11, TextColor = Color.FromArgb("#7A8485") });

            card.Content = stack;
            ModelosStackLayout.Children.Add(card);
        }
    }

    // ==================== APPLE TOAST ====================

    private async Task ShowToastAsync(string message)
    {
        AppleToastMessage.Text = message;
        AppleToast.IsVisible = true;
        _ = AppleToast.FadeToAsync(1.0, 200);
        await AppleToast.TranslateToAsync(0, 10, 200, Easing.CubicOut);
        await Task.Delay(2000);
        _ = AppleToast.FadeToAsync(0, 200);
        await AppleToast.TranslateToAsync(0, 0, 200, Easing.CubicIn);
        AppleToast.IsVisible = false;
    }
}
