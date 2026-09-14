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
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _proyectoActual = UserSession.ActiveProject;
        if (_proyectoActual == null)
        {
            LblProyectoDesign.Text = "Ninguno";
            return;
        }

        LblProyectoDesign.Text = _proyectoActual.Nombre;
        CargarDatosEspacio();
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

        foreach (var elem in elementos)
        {
            var btnDelete = new Border
            {
                BackgroundColor = Color.FromArgb("#FFF0F0"),
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
                var success = await ApiService.EliminarElementoEstructuralAsync(elem.Idelementoestructural);
                if (success.Success) await CargarElementosEstructurales(idVersion);
            };
            btnDelete.GestureRecognizers.Add(tapDelete);

            var infoStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
            infoStack.Children.Add(new Label { Text = elem.Tipo, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#001D39") });
            infoStack.Children.Add(new Label { Text = $"Alto: {elem.Dimensionalto} - Ancho: {elem.Dimensionancho}", FontSize = 12, TextColor = Color.FromArgb("#6EA2B3") });

            var row = new Grid { ColumnDefinitions = new ColumnDefinitionCollection { new ColumnDefinition { Width = GridLength.Star }, new ColumnDefinition { Width = GridLength.Auto } }, Padding = new Thickness(15), ColumnSpacing = 10 };
            row.Children.Add(infoStack);
            Grid.SetColumn(btnDelete, 1);
            row.Children.Add(btnDelete);

            var card = new Border
            {
                BackgroundColor = Color.FromArgb("#FFFFFF"),
                StrokeThickness = 0,
                Margin = new Thickness(0,0,0,10),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(12) }
            };
            card.Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.05f, Offset = new Point(0,2), Radius = 5 };
            card.Content = row;

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
        if (_proyectoActual == null)
        {
            await ShowAlertAsync("Aviso", "Selecciona un proyecto primero en Inicio.", "OK");
            return;
        }

        if (_espacioActual == null)
        {
            var req = new CrearEspacioFisicoRequest
            {
                Idproyecto = _proyectoActual.Idproyecto,
                Altoaproximado = ParseDecimal(AltoEntry.Text),
                Anchoaproximado = ParseDecimal(AnchoEntry.Text),
                Largoaproximado = ParseDecimal(LargoEntry.Text),
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
            _espacioActual.Altoaproximado = ParseDecimal(AltoEntry.Text);
            _espacioActual.Anchoaproximado = ParseDecimal(AnchoEntry.Text);
            _espacioActual.Largoaproximado = ParseDecimal(LargoEntry.Text);
            var res = await ApiService.ActualizarEspacioFisicoAsync(_espacioActual.Idespaciofisico, _espacioActual);
            if (res.Success) await ShowAlertAsync("Éxito", "Espacio actualizado", "OK");
        }
    }

    private async void OnAddElementoClicked(object sender, EventArgs e)
    {
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

    private async void OnImportModelClicked(object sender, EventArgs e)
    {
        if (_espacioActual == null || (_espacioActual.Anchoaproximado == 0 && _espacioActual.Largoaproximado == 0))
        {
            await ShowAlertAsync("Aviso", "Debes guardar el perímetro de la planta (Paso 1) antes de importar mobiliario.", "OK");
            return;
        }
        await ShowAlertAsync("Importar 3D", "Aquí se abrirá el explorador de archivos para importar modelos (.glb, .obj).", "OK");
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

    private Task ShowAlertAsync(string title, string message, string cancel)
    {
        return Application.Current!.Windows[0].Page!.DisplayAlertAsync(title, message, cancel);
    }
}

