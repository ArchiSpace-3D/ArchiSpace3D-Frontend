using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace MauiApp1.Views;

public partial class MeasureARPage : ContentPage
{
    private readonly ObservableCollection<MediciónDto> _mediciones = new();
    private List<ProyectoDto> _misProyectos = new();
    private bool _cameraLoaded;
    private bool _subscribedSaveEvent;

    public MeasureARPage()
    {
        InitializeComponent();
        MeasurementsCollectionView.ItemsSource = _mediciones;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_subscribedSaveEvent)
        {
            SaveMeasurementPage.OnMedicionGuardadaLocal += OnMedicionGuardadaHandler;
            _subscribedSaveEvent = true;
        }

        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
                status = await Permissions.RequestAsync<Permissions.Camera>();
        }
        catch { }

        await CargarProyectosAsync();

        if (UserSession.ActiveProject == null && _misProyectos.Count > 0)
            UserSession.ActiveProject = _misProyectos.FirstOrDefault();

        ActualizarProyectoUI();

        if (UserSession.ActiveProject != null)
        {
            if (!_cameraLoaded)
                CargarCamaraAR();
            await CargarMedicionesAsync();
        }
        else
        {
            NoProjectOverlay.IsVisible = true;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_subscribedSaveEvent)
        {
            SaveMeasurementPage.OnMedicionGuardadaLocal -= OnMedicionGuardadaHandler;
            _subscribedSaveEvent = false;
        }
    }

    private async void OnMedicionGuardadaHandler()
    {
        await CargarMedicionesAsync();
    }

    // ====================== CÁMARA & BRIDGE MULTIPLATAFORMA ======================
    private void CargarCamaraAR()
    {
        NoProjectOverlay.IsVisible = false;
        _cameraLoaded = true;

#if ANDROID
        ArWebView.Source = new UrlWebViewSource { Url = "file:///android_asset/ar_viewer.html" };
#elif IOS || MACCATALYST
        ArWebView.Source = new UrlWebViewSource { Url = Foundation.NSBundle.MainBundle.BundlePath + "/ar_viewer.html" };
#else
        ArWebView.Source = new UrlWebViewSource { Url = "ar_viewer.html" };
#endif
    }

    private void OnArWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Url)) return;

        if (e.Url.StartsWith("archispace://medida", StringComparison.OrdinalIgnoreCase))
        {
            e.Cancel = true;
            try
            {
                var uri = new Uri(e.Url);
                var query = uri.Query.TrimStart('?');
                foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var kv = pair.Split('=', 2);
                    if (kv.Length == 2 && kv[0] == "d")
                    {
                        var val = Uri.UnescapeDataString(kv[1]);
                        GuardarMedidaAutomatica(val);
                        break;
                    }
                }
            }
            catch { }
        }
    }

    // ====================== PROYECTO ======================
    private void ActualizarProyectoUI()
    {
        if (UserSession.ActiveProject == null)
        {
            LblProyectoActivo.Text = "Sin proyecto seleccionado";
            NoProjectOverlay.IsVisible = true;
        }
        else
        {
            LblProyectoActivo.Text = UserSession.ActiveProject.Nombre;
            NoProjectOverlay.IsVisible = false;
        }
    }

    private async Task CargarProyectosAsync()
    {
        try
        {
            _misProyectos = UserSession.EsArquitecto
                ? await ApiService.GetProyectosByArquitectoAsync(UserSession.Idusuario)
                : await ApiService.GetProyectosByClienteAsync(UserSession.Idusuario);
        }
        catch
        {
            _misProyectos = new();
        }
    }

    private async void OnSelectProjectClicked(object? sender, EventArgs e)
    {
        ProjectsSelectionList.Children.Clear();
        bool isDark = Application.Current?.RequestedTheme == AppTheme.Dark;

        if (_misProyectos == null || _misProyectos.Count == 0)
        {
            ProjectsSelectionList.Children.Add(new Label
            {
                Text = "No hay proyectos disponibles.",
                FontSize = 14,
                TextColor = Color.FromArgb("#8E8E93"),
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 30)
            });
        }
        else
        {
            foreach (var p in _misProyectos)
            {
                var row = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitionCollection
                    {
                        new ColumnDefinition { Width = GridLength.Star },
                        new ColumnDefinition { Width = GridLength.Auto }
                    },
                    Padding = new Thickness(20, 16)
                };

                var stack = new VerticalStackLayout { Spacing = 2 };
                stack.Children.Add(new Label
                {
                    Text = p.Nombre,
                    FontSize = 15,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb(isDark ? "#FFFFFF" : "#000000")
                });
                stack.Children.Add(new Label
                {
                    Text = string.IsNullOrWhiteSpace(p.Ubicacion) ? "Sin ubicación" : p.Ubicacion,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#8E8E93")
                });

                row.Children.Add(stack);

                if (UserSession.ActiveProject?.Idproyecto == p.Idproyecto)
                {
                    var checkIcon = new Image
                    {
                        Source = "ic_check.svg",
                        WidthRequest = 20,
                        HeightRequest = 20,
                        VerticalOptions = LayoutOptions.Center
                    };
                    Grid.SetColumn(checkIcon, 1);
                    row.Children.Add(checkIcon);
                }

                var border = new Border
                {
                    BackgroundColor = Colors.Transparent,
                    StrokeThickness = 0,
                    Content = row
                };

                var tap = new TapGestureRecognizer();
                tap.Tapped += async (s, ev) =>
                {
                    UserSession.ActiveProject = p;
                    ActualizarProyectoUI();
                    await CloseSelectProjectSheetAsync();

                    if (!_cameraLoaded)
                        CargarCamaraAR();
                    await CargarMedicionesAsync();
                    await ShowToastAsync($"Proyecto: {p.Nombre}");
                };
                border.GestureRecognizers.Add(tap);

                ProjectsSelectionList.Children.Add(border);

                ProjectsSelectionList.Children.Add(new BoxView
                {
                    HeightRequest = 1,
                    Color = Color.FromArgb(isDark ? "#38383A" : "#E5E5EA"),
                    Margin = new Thickness(20, 0, 20, 0)
                });
            }
        }

        SelectProjectBackdrop.IsVisible = true;
        await SelectProjectBackdrop.FadeToAsync(1, 200);
        SelectProjectSheetModal.IsVisible = true;
        await Task.WhenAll(
            SelectProjectSheetModal.FadeToAsync(1, 250),
            SelectProjectSheetModal.TranslateToAsync(0, 0, 300, Easing.CubicOut)
        );
    }

    private async void OnCloseSelectProjectSheetTapped(object? sender, TappedEventArgs e)
    {
        await CloseSelectProjectSheetAsync();
    }

    private async Task CloseSelectProjectSheetAsync()
    {
        await Task.WhenAll(
            SelectProjectSheetModal.FadeToAsync(0, 200),
            SelectProjectSheetModal.TranslateToAsync(0, 500, 250, Easing.CubicIn)
        );
        SelectProjectSheetModal.IsVisible = false;
        await SelectProjectBackdrop.FadeToAsync(0, 200);
        SelectProjectBackdrop.IsVisible = false;
    }

    // ====================== MEDICIONES ======================
    private async Task CargarMedicionesAsync()
    {
        if (UserSession.ActiveProject == null)
        {
            _mediciones.Clear();
            return;
        }

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        try
        {
            var mediciones = await ApiService.GetMediciónesByProyectoAsync(UserSession.ActiveProject.Idproyecto);

            _mediciones.Clear();
            if (mediciones != null)
            {
                foreach (var m in mediciones)
                    _mediciones.Add(m);
            }
        }
        catch { }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnNuevaMedidaManualTapped(object? sender, TappedEventArgs e)
    {
        if (UserSession.ActiveProject == null)
        {
            await ShowToastAsync("Selecciona un proyecto primero");
            return;
        }
        await Navigation.PushModalAsync(new SaveMeasurementPage(0));
    }

    private async void OnEliminarMedicionTapped(object? sender, TappedEventArgs e)
    {
        if (!UserSession.EsArquitecto || e.Parameter is not MediciónDto med) return;

        bool confirm = await AlertService.ShowAlertAsync("Eliminar medición", "¿Deseas eliminar este registro de medición?", "Eliminar", "Cancelar");
        if (!confirm) return;

        var (ok, msg) = await ApiService.EliminarMediciónAsync(med.Idmedicion);
        if (ok)
        {
            _mediciones.Remove(med);
            await ShowToastAsync("Medición eliminada");
        }
        else
        {
            await AlertService.ShowAlertAsync("Error", msg, "OK");
        }
    }

    // ====================== BOTONES HEADER ======================
    private async void OnBackTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//DashboardPage");
    }

    private async void OnRefreshTapped(object? sender, TappedEventArgs e)
    {
        await CargarMedicionesAsync();

        if (UserSession.ActiveProject != null)
            CargarCamaraAR();

        await ShowToastAsync("Actualizado");
    }

    // ====================== GUARDAR MEDIDA ======================
    public void GuardarMedidaAutomatica(string distanciaStr)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (UserSession.ActiveProject == null)
            {
                await ShowToastAsync("No hay proyecto activo.");
                return;
            }

            decimal distanciaMeters = 0;
            string cleanStr = distanciaStr.Replace(",", ".").Trim();

            if (cleanStr.EndsWith("cm", StringComparison.OrdinalIgnoreCase))
            {
                if (decimal.TryParse(cleanStr[..^2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d))
                    distanciaMeters = d / 100m;
            }
            else
            {
                cleanStr = cleanStr.Replace("m", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (decimal.TryParse(cleanStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d))
                    distanciaMeters = d;
            }

            if (distanciaMeters > 0)
            {
                await Navigation.PushModalAsync(new SaveMeasurementPage(distanciaMeters));
            }
        });
    }

    public async Task ShowToastAsync(string message)
    {
        AppleToastMessage.Text = message;
        AppleToast.IsVisible = true;
        await AppleToast.FadeToAsync(1, 300);
        await Task.Delay(2500);
        await AppleToast.FadeToAsync(0, 300);
        AppleToast.IsVisible = false;
    }
}
