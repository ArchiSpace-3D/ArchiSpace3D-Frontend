using System.Threading.Tasks;
using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Maui.Controls;

namespace MauiApp1.Views;

public partial class MeasurementsPage : ContentPage
{
    public MeasurementsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CargarMedicionesAsync();
    }

    private async Task CargarMedicionesAsync()
    {
        var proyecto = UserSession.ActiveProject;
        if (proyecto == null)
        {
            LblProyectoActivo.Text = "Sin proyecto seleccionado";
            MeasurementsCollectionView.ItemsSource = null;
            return;
        }

        LblProyectoActivo.Text = proyecto.Nombre;

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        MeasurementsCollectionView.ItemsSource = null;

        var mediciones = await ApiService.GetMediciónesByProyectoAsync(proyecto.Idproyecto);

        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        MeasurementsCollectionView.ItemsSource = mediciones;
    }

    private async void OnEliminarMedicionTapped(object? sender, TappedEventArgs e)
    {
        if (!UserSession.EsArquitecto || e.Parameter is not MediciónDto med) return;
        bool confirm = await AlertService.ShowAlertAsync("Eliminar medición", "¿Deseas eliminar esta medición?", "Eliminar", "Cancelar");
        if (!confirm) return;

        var (ok, msg) = await ApiService.EliminarMediciónAsync(med.Idmedicion);
        if (ok)
            await CargarMedicionesAsync();
        else
            await AlertService.ShowAlertAsync("Error", msg, "OK");
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//DashboardPage");
    }

    private async void OnRefreshTapped(object? sender, TappedEventArgs e)
    {
        await CargarMedicionesAsync();
    }
}
