using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MauiApp1.Views;

public partial class SpacesPage : ContentPage
{
    private readonly ObservableCollection<EspacioFisicoDto> _espacios = new();
    private readonly int _idProyecto;

    public SpacesPage(int idProyecto)
    {
        InitializeComponent();
        _idProyecto = idProyecto;
        SpacesCollectionView.ItemsSource = _espacios;
        BtnNuevoEspacio.IsVisible = UserSession.EsArquitecto;
        LblEmptySpacesSubtitle.Text = UserSession.EsArquitecto
            ? "Añade las habitaciones o zonas del proyecto."
            : "El arquitecto aún no ha registrado áreas en este proyecto.";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BtnNuevoEspacio.IsVisible = UserSession.EsArquitecto;
        CargarEspacios();
    }

    private async void CargarEspacios()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        _espacios.Clear();

        try
        {
            var espacios = await ApiService.GetEspaciosPorProyectoListaAsync(_idProyecto);
            if (espacios != null)
            {
                foreach (var e in espacios.Where(x => !string.Equals(x.Descripcion, "__PLANO_CAD__", StringComparison.OrdinalIgnoreCase)))
                {
                    _espacios.Add(e);
                }
            }
        }
        catch { }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnNewSpaceClicked(object? sender, EventArgs e)
    {
        if (!UserSession.EsArquitecto) return;

        EntryDesc.Text = "";
        EntryLargo.Text = "";
        EntryAncho.Text = "";
        EntryAlto.Text = "2.5";

        SheetBackdrop.IsVisible = true;
        SheetBackdrop.InputTransparent = false;
        await SheetBackdrop.FadeToAsync(1, 200);
        NewSpaceSheet.IsVisible = true;
        await NewSpaceSheet.TranslateToAsync(0, 0, 250, Easing.SpringOut);
        await NewSpaceSheet.FadeToAsync(1, 250);
    }

    private async void OnCloseSheetClicked(object? sender, EventArgs e)
    {
        await CloseSheetAsync();
    }

    private async Task CloseSheetAsync()
    {
        SheetBackdrop.InputTransparent = true;
        await Task.WhenAll(
            NewSpaceSheet.TranslateToAsync(0, 500, 250, Easing.CubicIn),
            NewSpaceSheet.FadeToAsync(0, 250),
            SheetBackdrop.FadeToAsync(0, 200)
        );
        NewSpaceSheet.IsVisible = false;
        SheetBackdrop.IsVisible = false;
    }

    private static decimal ParseDecimalSafe(string? val)
    {
        if (string.IsNullOrWhiteSpace(val)) return 0;
        val = val.Replace(",", ".");
        return decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private async void OnSubmitSpaceClicked(object? sender, EventArgs e)
    {
        if (!UserSession.EsArquitecto) return;

        if (string.IsNullOrWhiteSpace(EntryDesc.Text))
        {
            await AlertService.ShowAlertAsync("Aviso", "Debe ingresar un nombre para el espacio.", "OK");
            return;
        }

        decimal alto = ParseDecimalSafe(EntryAlto.Text);
        if (alto <= 0) alto = 2.5m;

        var req = new CrearEspacioFisicoRequest
        {
            Idproyecto = _idProyecto,
            Descripcion = EntryDesc.Text.Trim(),
            Largoaproximado = ParseDecimalSafe(EntryLargo.Text),
            Anchoaproximado = ParseDecimalSafe(EntryAncho.Text),
            Altoaproximado = alto,
            Puntosreferencia = "[]"
        };

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        var (success, msg, data) = await ApiService.GuardarEspacioFisicoAsync(req);
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        if (success && data != null)
        {
            _espacios.Add(data);
            await CloseSheetAsync();
        }
        else
        {
            await AlertService.ShowAlertAsync("Error", msg, "OK");
        }
    }

    private async void OnDeleteSpaceClicked(object? sender, EventArgs e)
    {
        if (!UserSession.EsArquitecto) return;

        if (sender is View btn && btn.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap && tap.CommandParameter is EspacioFisicoDto espacio)
        {
            bool confirm = await AlertService.ShowAlertAsync("Aviso", $"¿Seguro que deseas eliminar el espacio '{espacio.Descripcion}'?", "Sí, eliminar", "Cancelar");
            if (!confirm) return;

            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            var (success, msg) = await ApiService.EliminarEspacioFisicoAsync(espacio.Idespaciofisico);
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            if (success)
            {
                _espacios.Remove(espacio);
            }
            else
            {
                await AlertService.ShowAlertAsync("Error", msg, "OK");
            }
        }
    }
}
