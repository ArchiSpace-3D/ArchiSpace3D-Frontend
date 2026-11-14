using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1.Views;

public partial class MeasurementsPage : ContentPage
{
    private readonly int? _idProyectoOverride;

    public MeasurementsPage(int? idProyecto = null)
    {
        InitializeComponent();
        _idProyectoOverride = idProyecto;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        BottomTabBar.IsVisible = Navigation.ModalStack.Count == 0;
        await CargarMedicionesAsync();
    }

    private async Task CargarMedicionesAsync()
    {
        var proyecto = UserSession.ActiveProject;
        int? idProyecto = _idProyectoOverride ?? proyecto?.Idproyecto;

        if (idProyecto == null)
        {
            LblProyectoActivo.Text = "Sin proyecto seleccionado";
            MeasurementsCollectionView.ItemsSource = null;
            return;
        }

        LblProyectoActivo.Text = proyecto?.Nombre ?? $"Proyecto #{idProyecto}";

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        MeasurementsCollectionView.ItemsSource = null;

        var mediciones = await ApiService.GetMedicionesByProyectoAsync(idProyecto.Value);

        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        MeasurementsCollectionView.ItemsSource = mediciones;
    }

    private async void OnEliminarMedicionTapped(object? sender, TappedEventArgs e)
    {
        if (!UserSession.EsArquitecto || e.Parameter is not MedicionDto med) return;
        bool confirm = await AlertService.ShowAlertAsync("Eliminar medición", "¿Deseas eliminar esta medición?", "Eliminar", "Cancelar");
        if (!confirm) return;

        var (ok, msg) = await ApiService.EliminarMedicionAsync(med.Idmedicion);
        if (ok)
            await CargarMedicionesAsync();
        else
            await AlertService.ShowAlertAsync("Error", msg, "OK");
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        if (Navigation.ModalStack.Count > 0)
        {
            await Navigation.PopModalAsync();
            return;
        }
        await Shell.Current.GoToAsync("//DashboardPage");
    }

    private async void OnRefreshTapped(object? sender, TappedEventArgs e)
    {
        await CargarMedicionesAsync();
    }
}
