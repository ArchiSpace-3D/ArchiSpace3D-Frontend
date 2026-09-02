using Microsoft.Maui.Devices.Sensors;
using System.Text.Json;

namespace MauiApp1
{
    public partial class ARPage : ContentPage
    {
        public ARPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            
            // Cargar el HTML local para Three.js
            await LoadArEngineAsync();

            // Iniciar sensores para sincronizar la orientación 3D
            StartSensors();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopSensors();
        }

        private async Task LoadArEngineAsync()
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("ar_viewer.html");
                using var reader = new StreamReader(stream);
                var htmlString = await reader.ReadToEndAsync();
                
                ArWebView.Source = new HtmlWebViewSource { Html = htmlString };
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"No se pudo cargar el motor AR: {ex.Message}", "OK");
            }
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
            
            // Iniciar cámara de fondo
            CameraView.IsDetecting = false; // Solo visualización
        }

        private void StopSensors()
        {
            try { OrientationSensor.Default.ReadingChanged -= Orientation_ReadingChanged; OrientationSensor.Default.Stop(); } catch { }
        }

        private void Orientation_ReadingChanged(object sender, OrientationSensorChangedEventArgs e)
        {
            var q = e.Reading.Orientation; // Quaternion
            SyncOrientationWithThreeJs(q.X, q.Y, q.Z, q.W);
        }

        private async void SyncOrientationWithThreeJs(float x, float y, float z, float w)
        {
            if (ArWebView.Source == null) return;

            try
            {
                // Enviar el cuaternión (rotación exacta 3D) al motor web
                string script = $"if (typeof updateOrientationFromMaui === 'function') {{ updateOrientationFromMaui({x.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {y.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {z.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {w.ToString(System.Globalization.CultureInfo.InvariantCulture)}); }}";
                
                await ArWebView.EvaluateJavaScriptAsync(script);
            }
            catch 
            { 
            }
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            StopSensors();
            await Navigation.PopModalAsync();
        }
    }
}
