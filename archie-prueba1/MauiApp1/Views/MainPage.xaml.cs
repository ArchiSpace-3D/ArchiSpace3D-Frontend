using MauiApp1.Models;
using MauiApp1.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MauiApp1.Views
{
    public partial class MainPage : ContentPage
    {
        private List<ProyectoDto> _misProyectos = new();

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            ActualizarProyectoActivoUI();
            await CargarProyectosAsync();
            await CargarMediciónesGuardadasAsync();
        }

        private void ActualizarProyectoActivoUI()
        {
            if (UserSession.ActiveProject == null)
            {
                LblLinkedProject.Text = "Ninguno (Mediciónes no se guardarán)";
            }
            else
            {
                LblLinkedProject.Text = UserSession.ActiveProject.Nombre;
            }
        }

        private async Task CargarProyectosAsync()
        {
            try
            {
                if (UserSession.Rol == "Arquitecto")
                {
                    _misProyectos = await ApiService.GetProyectosByArquitectoAsync(UserSession.Idusuario);
                }
                else
                {
                    _misProyectos = await ApiService.GetProyectosByClienteAsync(UserSession.Idusuario);
                }
            }
            catch { }
        }

        private async void OnSelectProjectClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
            
            ProjectsSelectionList.Children.Clear();
            if (_misProyectos == null || _misProyectos.Count == 0)
            {
                ProjectsSelectionList.Children.Add(new Label { Text = "No hay proyectos disponibles.", TextColor = Colors.Gray, HorizontalOptions = LayoutOptions.Center });
            }
            else
            {
                foreach (var p in _misProyectos)
                {
                    var prjBtn = new Button
                    {
                        Text = p.Nombre,
                        BackgroundColor = Color.FromArgb("#F4F6F8"),
                        TextColor = Color.FromArgb("#0A4174"),
                        CornerRadius = 12,
                        HeightRequest = 48,
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold,
                        CommandParameter = p
                    };
                    prjBtn.Clicked += async (s, ev) =>
                    {
                        UserSession.ActiveProject = p;
                        ActualizarProyectoActivoUI();
                        await CargarMediciónesGuardadasAsync();
                        await CloseSelectProjectSheet();
                        await ShowToastAsync($"Proyecto activo: {p.Nombre}");
                    };
                    ProjectsSelectionList.Children.Add(prjBtn);
                }
            }

            SelectProjectSheetModal.IsVisible = true;
            await SelectProjectBackdrop.FadeToAsync(1, 200);
            await SelectProjectSheetCard.TranslateToAsync(0, 0, 350, Easing.CubicOut);
        }

        private async void OnCloseSelectProjectSheetClicked(object? sender, EventArgs e)
        {
            await CloseSelectProjectSheet();
        }

        private async Task CloseSelectProjectSheet()
        {
            await SelectProjectSheetCard.TranslateToAsync(0, 600, 250, Easing.CubicIn);
            await SelectProjectBackdrop.FadeToAsync(0, 200);
            SelectProjectSheetModal.IsVisible = false;
        }

        private async void OnRefreshMediciónesClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement img) { await img.ScaleToAsync(0.8, 50); await img.ScaleToAsync(1.0, 50); }
            await CargarMediciónesGuardadasAsync();
            await ShowToastAsync("Mediciónes actualizadas");
        }

        private async Task CargarMediciónesGuardadasAsync()
        {
            MediciónesGuardadasStack.Children.Clear();

            if (UserSession.ActiveProject == null || !UserSession.IsAuthenticated)
            {
                MediciónesGuardadasStack.Children.Add(new Label
                {
                    Text = "Selecciona un proyecto e inicia sesión para ver mediciones.",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#6EA2B3"),
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 4)
                });
                return;
            }

            try
            {
                var mediciones = await ApiService.GetMediciónesByProyectoAsync(UserSession.ActiveProject.Idproyecto);

                if (mediciones == null || mediciones.Count == 0)
                {
                    MediciónesGuardadasStack.Children.Add(new Label
                    {
                        Text = "No hay mediciones guardadas en este proyecto.",
                        FontSize = 12,
                        TextColor = Color.FromArgb("#6EA2B3"),
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 4)
                    });
                    return;
                }

                foreach (var m in mediciones)
                {
                    var card = new Border
                    {
                        BackgroundColor = Color.FromArgb("#E8F1F8"),
                        Stroke = Color.FromArgb("#BDD8E9"),
                        StrokeThickness = 1,
                        Padding = new Thickness(14, 10),
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) }
                    };

                    var grid = new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitionCollection
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Auto }
                        }
                    };

                    var stack = new VerticalStackLayout { Spacing = 2 };

                    string titulo = "Medición General";
                    if (!string.IsNullOrWhiteSpace(m.Etapa) || !string.IsNullOrWhiteSpace(m.Partida))
                    {
                        titulo = $"{m.Etapa ?? ""} {(!string.IsNullOrWhiteSpace(m.Etapa) && !string.IsNullOrWhiteSpace(m.Partida) ? "-" : "")} {m.Partida ?? ""}".Trim();
                    }

                    stack.Children.Add(new Label { Text = titulo, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#001D39") });
                    
                    string detalleTotal = $"Total: {m.Totalparcial?.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) ?? "0.00"} {m.Unidad ?? "m"}";
                    if (m.Veces > 1) detalleTotal += $" ({m.Veces} veces)";
                    
                    stack.Children.Add(new Label { Text = detalleTotal, FontSize = 13, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#129740") });
                    stack.Children.Add(new Label { Text = $"Registrada: {m.FechaFormateada}", FontSize = 11, TextColor = Color.FromArgb("#6EA2B3") });

                    grid.Children.Add(stack);

                    if (UserSession.Rol == "Arquitecto")
                    {
                        var delBtn = new Image
                        {
                            Source = "ic_trash.svg",
                            WidthRequest = 18,
                            HeightRequest = 18,
                            VerticalOptions = LayoutOptions.Center
                        };
                        int idMed = m.Idmedicion;
                        var tap = new TapGestureRecognizer();
                        tap.Tapped += async (s, ev) =>
                        {
                            await ApiService.EliminarMediciónAsync(idMed);
                            await CargarMediciónesGuardadasAsync();
                            await ShowToastAsync("Medición eliminada del proyecto");
                        };
                        delBtn.GestureRecognizers.Add(tap);
                        Grid.SetColumn(delBtn, 1);
                        grid.Children.Add(delBtn);
                    }

                    card.Content = grid;
                    MediciónesGuardadasStack.Children.Add(card);
                }
            }
            catch {}
        }

        private async void OnRefreshBatteryClicked(object? sender, EventArgs e)
        {
            BatteryLabel.Text = $"Nivel: {Battery.Default.ChargeLevel * 100}%";
            await ShowToastAsync("Batería actualizada");
        }

                private void OnToggleAccelClicked(object? sender, EventArgs e)
        {
            if (Accelerometer.Default.IsSupported)
            {
                if (!Accelerometer.Default.IsMonitoring)
                {
                    Accelerometer.Default.ReadingChanged += Accelerometer_ReadingChanged;
                    Accelerometer.Default.Start(SensorSpeed.UI);
                    BtnToggleAccel.Text = "Detener";
                }
                else
                {
                    Accelerometer.Default.Stop();
                    Accelerometer.Default.ReadingChanged -= Accelerometer_ReadingChanged;
                    BtnToggleAccel.Text = "Activar";
                    AccelLabel.Text = "Inactivo";
                }
            }
        }
        private void Accelerometer_ReadingChanged(object? sender, AccelerometerChangedEventArgs e)
        {
            AccelLabel.Text = $"X: {e.Reading.Acceleration.X:F2}, Y: {e.Reading.Acceleration.Y:F2}, Z: {e.Reading.Acceleration.Z:F2}";
        }

        private void OnToggleGyroClicked(object? sender, EventArgs e)
        {
            if (Gyroscope.Default.IsSupported)
            {
                if (!Gyroscope.Default.IsMonitoring)
                {
                    Gyroscope.Default.ReadingChanged += Gyroscope_ReadingChanged;
                    Gyroscope.Default.Start(SensorSpeed.UI);
                    BtnToggleGyro.Text = "Detener";
                }
                else
                {
                    Gyroscope.Default.Stop();
                    Gyroscope.Default.ReadingChanged -= Gyroscope_ReadingChanged;
                    BtnToggleGyro.Text = "Activar";
                    GyroLabel.Text = "Inactivo";
                }
            }
        }
        private void Gyroscope_ReadingChanged(object? sender, GyroscopeChangedEventArgs e)
        {
            GyroLabel.Text = $"X: {e.Reading.AngularVelocity.X:F2}, Y: {e.Reading.AngularVelocity.Y:F2}, Z: {e.Reading.AngularVelocity.Z:F2}";
        }

        private void OnToggleCompassClicked(object? sender, EventArgs e)
        {
            if (Compass.Default.IsSupported)
            {
                if (!Compass.Default.IsMonitoring)
                {
                    Compass.Default.ReadingChanged += Compass_ReadingChanged;
                    Compass.Default.Start(SensorSpeed.UI);
                    BtnToggleCompass.Text = "Detener";
                }
                else
                {
                    Compass.Default.Stop();
                    Compass.Default.ReadingChanged -= Compass_ReadingChanged;
                    BtnToggleCompass.Text = "Activar";
                    CompassLabel.Text = "Inactivo";
                }
            }
        }
        private void Compass_ReadingChanged(object? sender, CompassChangedEventArgs e)
        {
            CompassLabel.Text = $"Heading: {e.Reading.HeadingMagneticNorth:F1}°";
        }

        private void OnToggleBarometerClicked(object? sender, EventArgs e)
        {
            if (Barometer.Default.IsSupported)
            {
                if (!Barometer.Default.IsMonitoring)
                {
                    Barometer.Default.ReadingChanged += Barometer_ReadingChanged;
                    Barometer.Default.Start(SensorSpeed.UI);
                    BtnToggleBarometer.Text = "Detener";
                }
                else
                {
                    Barometer.Default.Stop();
                    Barometer.Default.ReadingChanged -= Barometer_ReadingChanged;
                    BtnToggleBarometer.Text = "Activar";
                    BarometerLabel.Text = "Inactivo";
                }
            }
        }
        private void Barometer_ReadingChanged(object? sender, BarometerChangedEventArgs e)
        {
            BarometerLabel.Text = $"Presión: {e.Reading.PressureInHectopascals:F2} hPa";
        }

        private async void OnOpenARCameraClicked(object? sender, EventArgs e) 
        { 
            if (MauiApp1.Services.UserSession.ActiveProject == null)
            {
                await ShowToastAsync("Selecciona un proyecto primero.");
                return;
            }

            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status == PermissionStatus.Granted)
            {
                await Navigation.PushModalAsync(new ARPage()); 
            }
            else
            {
                await ShowToastAsync("Permiso de cámara denegado.");
            }
        }

        private async void OnManualMeasurementClicked(object? sender, EventArgs e) 
        { 
            if (MauiApp1.Services.UserSession.ActiveProject == null)
            {
                await ShowToastAsync("Selecciona un proyecto primero.");
                return;
            }

            await Navigation.PushModalAsync(new SaveMeasurementPage(0));
        }
        private async Task ShowToastAsync(string message)
        {
            AppleToastMessage.Text = message;
            AppleToast.IsVisible = true;
            await AppleToast.FadeToAsync(1, 200);
            await Task.Delay(2500);
            await AppleToast.FadeToAsync(0, 200);
            AppleToast.IsVisible = false;
        }
    }
}

