using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;
using System.Globalization;

namespace MauiApp1.Views;

public partial class SpacesPage : ContentPage
{
    private readonly ObservableCollection<EspacioFisicoDto> _espacios = new();
    private int _idProyecto;

    public SpacesPage(int idProyecto)
    {
        InitializeComponent();
        _idProyecto = idProyecto;
        SpacesCollectionView.ItemsSource = _espacios;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarEspacios();
    }

    private async void CargarEspacios()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        _espacios.Clear();

        try
        {
            var espacios = await ApiService.GetEspaciosFisicosAsync();
            if (espacios != null)
            {
                foreach (var e in espacios.Where(x => x.Idproyecto == _idProyecto))
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
        EntryDesc.Text = "";
        EntryLargo.Text = "";
        EntryAncho.Text = "";
        
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

    private decimal ParseDecimalSafe(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return 0;
        val = val.Replace(",", ".");
        if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;
        return 0;
    }

    private async void OnSubmitSpaceClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EntryDesc.Text))
        {
            await DisplayAlert("Error", "Debe ingresar un nombre para el espacio.", "OK");
            return;
        }

        var req = new CrearEspacioFisicoRequest
        {
            Idproyecto = _idProyecto,
            Descripcion = EntryDesc.Text,
            Largoaproximado = ParseDecimalSafe(EntryLargo.Text),
            Anchoaproximado = ParseDecimalSafe(EntryAncho.Text),
            Altoaproximado = 2.5m, // Default height
            Puntosreferencia = "[]",
            
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
            await DisplayAlert("Error", msg, "OK");
        }
    }

    private async void OnDeleteSpaceClicked(object? sender, EventArgs e)
    {
        if (sender is Image btn && btn.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap && tap.CommandParameter is EspacioFisicoDto espacio)
        {
            bool confirm = await DisplayAlert("Eliminar", $"¿Seguro que deseas eliminar el espacio '{espacio.Descripcion}'?", "Sí", "No");
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
                await DisplayAlert("Error", msg, "OK");
            }
        }
    }
}
