using Microsoft.Maui.Devices.Sensors;
using System.Text.Json;

namespace MauiApp1
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
                handler.PlatformView.SetWebChromeClient(new MyWebChromeClient());
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
                    await DisplayAlertAsync("Error", "Se requiere cámara", "OK");
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
                string script = $"if (typeof updateOrientationFromMaui === 'function') {{ updateOrientationFromMaui({x.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {y.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {z.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {w.ToString(System.Globalization.CultureInfo.InvariantCulture)}); }}";
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
        private async void OnGuardarMedidaClicked(object? sender, EventArgs e)
        {
            if (UserSession.ActiveProject == null)
            {
                await DisplayAlert("Aviso", "No hay un proyecto activo para guardar la medida.", "OK");
                return;
            }

            string result = await DisplayPromptAsync("Guardar Medida", "Ingresa la distancia medida en metros (ej. 2.45):", "Guardar", "Cancelar", keyboard: Keyboard.Numeric);
            
            if (!string.IsNullOrWhiteSpace(result) && decimal.TryParse(result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal distancia))
            {
                var req = new MauiApp1.Models.CrearMedicionRequest
                {
                    Idproyecto = UserSession.ActiveProject.Idproyecto,
                    Distancia = distancia,
                    Puntoinicial = "{\"x\":0, \"y\":0, \"z\":0}",
                    Puntofinal = "{\"x\":0, \"y\":0, \"z\":0}",
                    Fechamedicion = DateTime.UtcNow
                };

                var (success, msg) = await ApiService.GuardarMedicionAsync(req);
                if (success)
                {
                    await DisplayAlert("Éxito", $"Medida de {distancia}m guardada en el proyecto {UserSession.ActiveProject.Nombre}.", "OK");
                }
                else
                {
                    await DisplayAlert("Error", $"No se guardó la medida: {msg}", "OK");
                }
            }
        }
    }

#if ANDROID
    public class MyWebChromeClient : Android.Webkit.WebChromeClient
    {
        public override void OnPermissionRequest(Android.Webkit.PermissionRequest? request)
        {
            try
            {
                request?.Grant(request.GetResources());
            }
            catch { }
        }
    }
#endif
}

