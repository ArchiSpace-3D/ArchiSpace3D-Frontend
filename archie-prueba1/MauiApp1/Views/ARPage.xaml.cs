using Microsoft.Maui.Devices.Sensors;
using System.Text.Json;
using MauiApp1.Services;
using MauiApp1.Models;
using System.Globalization;

namespace MauiApp1.Views
{
    public partial class ARPage : ContentPage
    {
        public ARPage()
        {
            InitializeComponent();
#if ANDROID
            Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("WebRTC", (handler, view) =>
            {
                handler.PlatformView.Settings.JavaScriptEnabled = true;
                handler.PlatformView.Settings.MediaPlaybackRequiresUserGesture = false;
                handler.PlatformView.Settings.AllowFileAccess = true;
                handler.PlatformView.Settings.AllowFileAccessFromFileURLs = true;
                handler.PlatformView.Settings.AllowUniversalAccessFromFileURLs = true;
                handler.PlatformView.SetWebChromeClient(new MyWebChromeClient(this));
            });
#endif
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
                    await AlertService.ShowAlertAsync("Error", "Se requiere cámara", "OK");
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

        private void StartSensors()
        {
            try {
                if (OrientationSensor.Default.IsSupported && !OrientationSensor.Default.IsMonitoring)
                {
                    OrientationSensor.Default.ReadingChanged += Orientation_ReadingChanged;
                    OrientationSensor.Default.Start(SensorSpeed.Game);
                }
            } catch { }
        }

        private void StopSensors()
        {
            try { 
                OrientationSensor.Default.ReadingChanged -= Orientation_ReadingChanged; 
                OrientationSensor.Default.Stop(); 
            } catch { }
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

        public async void GuardarMedidaAutomatica(string distanciaStr)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (UserSession.ActiveProject == null)
                {
                    await AlertService.ShowAlertAsync("Aviso", "No hay un proyecto activo para guardar la medida.", "OK");
                    return;
                }

                if (decimal.TryParse(distanciaStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal distancia))
                {
                    var req = new CrearMediciónRequest
                    {
                        Idproyecto = UserSession.ActiveProject.Idproyecto,
                        Distancia = distancia,
                        Puntoinicial = "{\"x\":0, \"y\":0, \"z\":0}",
                        Puntofinal = "{\"x\":0, \"y\":0, \"z\":0}",
                        Fechamedicion = DateTime.UtcNow
                    };

                    var (success, msg) = await ApiService.GuardarMediciónAsync(req);
                    if (success)
                    {
                        await AlertService.ShowAlertAsync("Éxito", $"Medida de {distancia}m guardada en el proyecto {UserSession.ActiveProject.Nombre}.", "OK");
                    }
                    else
                    {
                        await AlertService.ShowAlertAsync("Error", $"No se guardó la medida: {msg}", "OK");
                    }
                }
            });
        }
    }

#if ANDROID
    public class MyWebChromeClient : Android.Webkit.WebChromeClient
    {
        private readonly ARPage _page;
        public MyWebChromeClient(ARPage page)
        {
            _page = page;
        }

        public override void OnPermissionRequest(Android.Webkit.PermissionRequest? request)
        {
            try
            {
                request?.Grant(request.GetResources());
            }
            catch { }
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
}


