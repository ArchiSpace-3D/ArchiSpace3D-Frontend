using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1;

public partial class NotificationsPage : ContentPage
{
    private readonly ObservableCollection<NotificacionDto> _notificaciones = new();

    public NotificationsPage()
    {
        InitializeComponent();
        NotificacionesCollectionView.ItemsSource = _notificaciones;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        CargarNotificaciones();
    }

    private async void CargarNotificaciones()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        _notificaciones.Clear();

        try
        {
            var notifs = await ApiService.GetNotificacionesByUsuarioAsync(UserSession.Idusuario);
            if (notifs != null)
            {
                foreach (var n in notifs)
                {
                    _notificaciones.Add(n);
                }
            }
        }
        catch { }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            NotifRefresh.IsRefreshing = false;
        }
    }

    private void OnRefresh(object sender, EventArgs e)
    {
        CargarNotificaciones();
    }

    private async void OnCloseClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (sender is Border btn && btn.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap && tap.CommandParameter is NotificacionDto notif)
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            var (success, _) = await ApiService.EliminarNotificacionAsync(notif.Idnotificacion);
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            if (success)
            {
                _notificaciones.Remove(notif);
            }
        }
    }
}
