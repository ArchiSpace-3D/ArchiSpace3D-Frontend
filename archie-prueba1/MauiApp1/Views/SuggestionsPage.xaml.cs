using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1.Views;

public partial class SuggestionsPage : ContentPage
{
    private readonly ObservableCollection<SugerenciaDto> _sugerencias = new();
    private int _idProyecto;

    public bool IsArquitecto => UserSession.Rol == "Arquitecto";

    public SuggestionsPage(int idProyecto)
    {
        InitializeComponent();
        _idProyecto = idProyecto;
        SuggestionsCollectionView.ItemsSource = _sugerencias;
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarSugerencias();
    }

    private async void CargarSugerencias()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        _sugerencias.Clear();

        try
        {
            var items = await ApiService.GetSugerenciasByProyectoAsync(_idProyecto);
            if (items != null)
            {
                foreach (var item in items)
                {
                    _sugerencias.Add(item);
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

    private async void OnNewSuggestionClicked(object? sender, EventArgs e)
    {
        EntryTitulo.Text = "";
        EntryDesc.Text = "";
        
        SheetBackdrop.IsVisible = true;
        SheetBackdrop.InputTransparent = false;
        await SheetBackdrop.FadeToAsync(1, 200);
        NewSuggestionSheet.IsVisible = true;
        await NewSuggestionSheet.TranslateToAsync(0, 0, 250, Easing.SpringOut);
        await NewSuggestionSheet.FadeToAsync(1, 250);
    }

    private async void OnCloseSheetClicked(object? sender, EventArgs e)
    {
        await CloseSheetAsync();
    }

    private async Task CloseSheetAsync()
    {
        SheetBackdrop.InputTransparent = true;
        await Task.WhenAll(
            NewSuggestionSheet.TranslateToAsync(0, 500, 250, Easing.CubicIn),
            NewSuggestionSheet.FadeToAsync(0, 250),
            SheetBackdrop.FadeToAsync(0, 200)
        );
        NewSuggestionSheet.IsVisible = false;
        SheetBackdrop.IsVisible = false;
    }

    private async void OnSubmitSuggestionClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EntryTitulo.Text) || string.IsNullOrWhiteSpace(EntryDesc.Text))
        {
            await DisplayAlertAsync("Error", "Debes ingresar un título y descripción.", "OK");
            return;
        }

        var req = new CrearSugerenciaRequest
        {
            Idproyecto = _idProyecto,
            Idusuario = UserSession.Idusuario,
            Titulo = EntryTitulo.Text,
            Descripcion = EntryDesc.Text
        };

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        var (success, msg, data) = await ApiService.CrearSugerenciaAsync(req);
        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        if (success && data != null)
        {
            _sugerencias.Insert(0, data);
            await CloseSheetAsync();
        }
        else
        {
            await DisplayAlertAsync("Error", msg, "OK");
        }
    }

    private async void OnApproveClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is SugerenciaDto sug)
            await UpdateEstado(sug, "Aprobada");
    }

    private async void OnRejectClicked(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is SugerenciaDto sug)
            await UpdateEstado(sug, "Rechazada");
    }

    private async Task UpdateEstado(SugerenciaDto sug, string nuevoEstado)
    {
        LoadingIndicator.IsRunning = true;
        var (success, msg) = await ApiService.ActualizarEstadoSugerenciaAsync(sug.Idsugerencia, nuevoEstado);
        LoadingIndicator.IsRunning = false;

        if (success)
        {
            sug.Estado = nuevoEstado;
            // Force UI update
            int idx = _sugerencias.IndexOf(sug);
            if (idx >= 0)
            {
                _sugerencias.RemoveAt(idx);
                _sugerencias.Insert(idx, sug);
            }
        }
        else
        {
            await DisplayAlertAsync("Error", msg, "OK");
        }
    }
}
