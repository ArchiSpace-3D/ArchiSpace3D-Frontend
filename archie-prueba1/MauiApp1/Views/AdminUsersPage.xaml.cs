using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;

namespace MauiApp1.Views;

public partial class AdminUsersPage : ContentPage
{
    private readonly ObservableCollection<UsuarioDto> _usuarios = new();

    public AdminUsersPage()
    {
        InitializeComponent();
        UsersCollectionView.ItemsSource = _usuarios;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!UserSession.EsArquitecto)
        {
            await Navigation.PopModalAsync();
            return;
        }
        CargarUsuarios();
    }

    private async void CargarUsuarios()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        _usuarios.Clear();

        try
        {
            var list = await ApiService.GetUsuariosAsync();
            if (list != null)
            {
                foreach (var u in list.Where(u => !string.Equals(u.Rol, "Arquitecto", StringComparison.OrdinalIgnoreCase)))
                {
                    _usuarios.Add(u);
                }
            }
        }
        catch { }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            UsersRefresh.IsRefreshing = false;
        }
    }

    private void OnRefresh(object? sender, EventArgs e)
    {
        CargarUsuarios();
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnDeleteUserClicked(object? sender, EventArgs e)
    {
        if (!UserSession.EsArquitecto) return;

        if (sender is Border btn && btn.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap && tap.CommandParameter is UsuarioDto user)
        {
            if (string.Equals(user.Rol, "Arquitecto", StringComparison.OrdinalIgnoreCase))
            {
                await ShowAlertAsync("Acción denegada", "No puedes eliminar a otros arquitectos del sistema.", "Entendido");
                return;
            }

            var confirm = await ShowAlertConfirmAsync("Aviso", $"¿Estás seguro de eliminar al cliente {user.NombreCompleto}?", "Sí, eliminar", "Cancelar");
            if (!confirm) return;

            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            var (success, msg) = await ApiService.EliminarUsuarioAsync(user.Idusuario);
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;

            if (success)
            {
                _usuarios.Remove(user);
            }
            else
            {
                await ShowAlertAsync("Error", msg, "OK");
            }
        }
    }

    private Task ShowAlertAsync(string title, string message, string cancel)
    {
        return AlertService.ShowAlertAsync(title, message, cancel);
    }

    private Task<bool> ShowAlertConfirmAsync(string title, string message, string accept, string cancel)
    {
        return AlertService.ShowAlertAsync(title, message, accept, cancel);
    }
}
