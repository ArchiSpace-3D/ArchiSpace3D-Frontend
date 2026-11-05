using MauiApp1.Services;
using System.Globalization;

namespace MauiApp1.Views;

public partial class ARPage : ContentPage
{
    public ARPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                await AlertService.ShowAlertAsync("Aviso", "Se requiere acceso a la cámara para usar la regla AR.", "OK");
                return;
            }
        }

        LoadArEngine();
        StartSensors();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopSensors();
    }

    private void LoadArEngine()
    {
#if ANDROID
        ArWebView.Source = new UrlWebViewSource { Url = "file:///android_asset/ar_viewer.html" };
#elif IOS
        ArWebView.Source = new UrlWebViewSource { Url = Foundation.NSBundle.MainBundle.BundlePath + "/ar_viewer.html" };
#else
        ArWebView.Source = new UrlWebViewSource { Url = "ar_viewer.html" };
#endif
    }

    private void OnArWebViewNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Url) || !e.Url.StartsWith("archispace://medida", StringComparison.OrdinalIgnoreCase))
            return;

        e.Cancel = true;
        try
        {
            var uri = new Uri(e.Url);
            var query = uri.Query.TrimStart('?');
            foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split('=', 2);
                if (kv.Length == 2 && kv[0] == "d")
                {
                    GuardarMedidaAutomatica(Uri.UnescapeDataString(kv[1]));
                    break;
                }
            }
        }
        catch { }
    }

    private void StartSensors()
    {
        try
        {
            if (OrientationSensor.Default.IsSupported && !OrientationSensor.Default.IsMonitoring)
            {
                OrientationSensor.Default.ReadingChanged += Orientation_ReadingChanged;
                OrientationSensor.Default.Start(SensorSpeed.Game);
            }
        }
        catch { }
    }

    private void StopSensors()
    {
        try
        {
            OrientationSensor.Default.ReadingChanged -= Orientation_ReadingChanged;
            OrientationSensor.Default.Stop();
        }
        catch { }
    }

    private void Orientation_ReadingChanged(object? sender, OrientationSensorChangedEventArgs e)
    {
        var q = e.Reading.Orientation;
        SyncOrientationWithThreeJs(q.X, q.Y, q.Z, q.W);
    }

    private async void SyncOrientationWithThreeJs(float x, float y, float z, float w)
    {
        if (ArWebView.Source == null) return;

        try
        {
            string script = $"if (typeof updateOrientationFromMaui === 'function') {{ updateOrientationFromMaui({x.ToString(CultureInfo.InvariantCulture)}, {y.ToString(CultureInfo.InvariantCulture)}, {z.ToString(CultureInfo.InvariantCulture)}, {w.ToString(CultureInfo.InvariantCulture)}); }}";
            await ArWebView.EvaluateJavaScriptAsync(script);
        }
        catch { }
    }

    private async void OnCloseClicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement btn)
        {
            await btn.ScaleToAsync(0.92, 50);
            await btn.ScaleToAsync(1.0, 50);
        }
        StopSensors();
        await Navigation.PopModalAsync();
    }

    public void GuardarMedidaAutomatica(string distanciaStr)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (UserSession.ActiveProject == null)
            {
                await AlertService.ShowAlertAsync("Aviso", "No hay un proyecto activo para guardar la medida.", "OK");
                return;
            }

            decimal distanciaMeters = 0;
            string cleanStr = distanciaStr.Replace(",", ".");
            if (cleanStr.Contains("cm", StringComparison.OrdinalIgnoreCase))
            {
                if (decimal.TryParse(cleanStr.Replace("cm", "", StringComparison.OrdinalIgnoreCase).Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d))
                    distanciaMeters = d / 100m;
            }
            else
            {
                if (decimal.TryParse(cleanStr.Replace("m", "", StringComparison.OrdinalIgnoreCase).Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d))
                    distanciaMeters = d;
            }

            if (distanciaMeters > 0)
            {
                await Navigation.PushModalAsync(new SaveMeasurementPage(distanciaMeters));
            }
        });
    }
}
