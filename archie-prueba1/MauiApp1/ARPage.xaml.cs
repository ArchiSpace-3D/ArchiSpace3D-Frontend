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
                    await DisplayAlert("Error", "Se requiere cámara", "OK");
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
