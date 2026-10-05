using MauiApp1.Models;
using MauiApp1.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace MauiApp1.Views;

public partial class MeasureARPage : ContentPage
{
    private readonly ObservableCollection<MediciónDto> _mediciones = new();
    private List<ProyectoDto> _misProyectos = new();

    public MeasureARPage()
    {
        InitializeComponent();
        MeasurementsCollectionView.ItemsSource = _mediciones;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Verificar permisos de cámara
        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status != PermissionStatus.Granted)
        {
            await ShowToastAsync("Se requiere permiso de cámara.");
            return;
        }

        ActualizarProyectoUI();
        await CargarProyectosAsync();

        // Solo cargar la cámara si hay proyecto activo
        if (UserSession.ActiveProject != null)
        {
            CargarCamaraAR();
            await CargarMedicionesAsync();
        }
        else
        {
            // Mostrar overlay pidiendo proyecto
            NoProjectOverlay.IsVisible = true;
        }

        // Asegurar que el WebView tenga el bridge configurado
        SetupWebViewBridge();
    }

    // ====================== CÁMARA ======================
    private void CargarCamaraAR()
    {
        NoProjectOverlay.IsVisible = false;

#if ANDROID
        ArWebView.Source = new UrlWebViewSource { Url = "file:///android_asset/ar_viewer.html" };
#elif IOS
        ArWebView.Source = new UrlWebViewSource { Url = Foundation.NSBundle.MainBundle.BundlePath + "/ar_viewer.html" };
#else
        ArWebView.Source = new UrlWebViewSource { Url = "ar_viewer.html" };
#endif
    }

    private void SetupWebViewBridge()
    {
#if ANDROID
        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("MeasureAR", (handler, view) =>
        {
            handler.PlatformView.Settings.JavaScriptEnabled = true;
            handler.PlatformView.Settings.MediaPlaybackRequiresUserGesture = false;
            handler.PlatformView.Settings.AllowFileAccess = true;
            handler.PlatformView.Settings.AllowFileAccessFromFileURLs = true;
            handler.PlatformView.Settings.AllowUniversalAccessFromFileURLs = true;
            handler.PlatformView.SetWebChromeClient(new MeasureWebChromeClient(this));
        });
#endif
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
            _misProyectos = UserSession.Rol == "Arquitecto"
                ? await ApiService.GetProyectosByArquitectoAsync(UserSession.Idusuario)
                : await ApiService.GetProyectosByClienteAsync(UserSession.Idusuario);
        }
        catch { _misProyectos = new(); }
    }

    private async void OnSelectProjectClicked(object? sender, EventArgs e)
    {
        ProjectsSelectionList.Children.Clear();

        if (_misProyectos == null || _misProyectos.Count == 0)
        {
            ProjectsSelectionList.Children.Add(new Label
            {
                Text = "No hay proyectos disponibles.",
                FontSize = 14,
                TextColor = Colors.Gray,
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
                    TextColor = Color.FromArgb("#000000")
                });
                stack.Children.Add(new Label
                {
                    Text = string.IsNullOrWhiteSpace(p.Ubicacion) ? "Sin ubicación" : p.Ubicacion,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#8E8E93")
                });

                row.Children.Add(stack);

                // Checkmark si es el activo
                if (UserSession.ActiveProject?.Idproyecto == p.Idproyecto)
                {
                    var check = new Label
                    {
                        Text = "✓",
                        FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#007AFF"),
                        VerticalOptions = LayoutOptions.Center
                    };
                    Grid.SetColumn(check, 1);
                    row.Children.Add(check);
                }

                // Borde inferior separador
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

                    // Recargar cámara con el nuevo proyecto
                    CargarCamaraAR();
                    await CargarMedicionesAsync();
                    await ShowToastAsync($"Proyecto: {p.Nombre}");
                };
                border.GestureRecognizers.Add(tap);

                ProjectsSelectionList.Children.Add(border);

                // Separador visual
                ProjectsSelectionList.Children.Add(new BoxView
                {
                    HeightRequest = 1,
                    Color = Color.FromArgb("#E5E5EA"),
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

    // ====================== GUARDAR MEDIDA (llamado desde el HTML) ======================
    public async void GuardarMedidaAutomatica(string distanciaStr)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (UserSession.ActiveProject == null)
            {
                await ShowToastAsync("No hay proyecto activo.");
                return;
            }

            decimal distanciaMeters = 0;
            string cleanStr = distanciaStr.Replace(",", ".");

            if (cleanStr.Contains("cm"))
            {
                if (decimal.TryParse(cleanStr.Replace("cm", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d))
                    distanciaMeters = d / 100m;
            }
            else
            {
                if (decimal.TryParse(cleanStr.Replace("m", "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d))
                    distanciaMeters = d;
            }

            if (distanciaMeters > 0)
            {
                // Al guardar, recargamos la lista y el modal de SaveMeasurementPage ya existe
                await Navigation.PushModalAsync(new SaveMeasurementPage(distanciaMeters));
            }
        });
    }

    // ====================== TOAST ======================
    public async Task ShowToastAsync(string message)
    {
        AppleToastMessage.Text = message;
        AppleToast.IsVisible = true;
        await AppleToast.FadeToAsync(1, 300);
        await Task.Delay(2500);
        await AppleToast.FadeToAsync(0, 300);
        AppleToast.IsVisible = false;
    }

    // Método para cuando SaveMeasurementPage regresa
    public async Task NotificarMedidaGuardadaAsync()
    {
        await CargarMedicionesAsync();
        await ShowToastAsync("Medición guardada");
    }
}

#if ANDROID
public class MeasureWebChromeClient : Android.Webkit.WebChromeClient
{
    private readonly MeasureARPage _page;
    public MeasureWebChromeClient(MeasureARPage page) { _page = page; }

    public override void OnPermissionRequest(Android.Webkit.PermissionRequest? request)
    {
        try { request?.Grant(request.GetResources()); } catch { }
    }

    public override bool OnJsPrompt(Android.Webkit.WebView? view, string? url, string? message, string? defaultValue, Android.Webkit.JsPromptResult? result)
    {
        if (message != null && message.StartsWith("ARCHIE_MEDIDA:"))
        {
            string value = message.Substring("ARCHIE_MEDIDA:".Length);
            _page.GuardarMedidaAutomatica(value);
            result?.Confirm("OK");
            return true;
        }
        return base.OnJsPrompt(view, url, message, defaultValue, result);
    }
}
#endif