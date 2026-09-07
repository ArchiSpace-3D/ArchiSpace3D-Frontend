using System.Text.Json;
using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Maui.Devices.Sensors;

namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
        private double _lastCalculatedObjectHeight = 0;
        private double _lastGpsDistanceMeters = 0;
        private Location? _lastGpsPointB;
        private double _lastPhotoSizeCm = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnEnterARClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { await btn.ScaleToAsync(0.96, 60); await btn.ScaleToAsync(1.0, 60); }
            await Navigation.PushModalAsync(new ARPage());
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateProjectDisplay();
            _ = CargarMedicionesGuardadasAsync();

            try 
            {
                CheckBattery();
                Battery.Default.BatteryInfoChanged += Battery_BatteryInfoChanged;
            }
            catch (FeatureNotSupportedException)
            {
            }
            catch (Exception ex) 
            { 
                BatteryLabel.Text = $"Error batería: {ex.Message}"; 
            }
        }

        private void UpdateProjectDisplay()
        {
            if (UserSession.ActiveProject != null)
            {
                CurrentProjectNameLabel.Text = $"{UserSession.ActiveProject.Nombre} ({UserSession.ActiveProject.EstadoNormalizado})";
            }
            else
            {
                CurrentProjectNameLabel.Text = "Ningún proyecto seleccionado";
            }
        }

        private async void OnSelectProjectClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { await btn.ScaleToAsync(0.92, 50); await btn.ScaleToAsync(1.0, 50); }
            if (!UserSession.IsAuthenticated)
            {
                await ShowToastAsync("Inicia sesión para seleccionar proyectos.");
                return;
            }

            var proyectos = await ApiService.GetProyectosAsync();
            if (proyectos == null || proyectos.Count == 0)
            {
                await ShowToastAsync("No hay proyectos registrados aún.");
                return;
            }

            ProjectsSelectionList.Children.Clear();
            foreach (var p in proyectos)
            {
                var card = new Border
                {
                    BackgroundColor = Color.FromArgb("#F5F3EF"),
                    Stroke = Color.FromArgb("#DCD7C9"),
                    StrokeThickness = 1,
                    Padding = new Thickness(16, 12),
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) }
                };

                var stack = new VerticalStackLayout { Spacing = 2 };
                stack.Children.Add(new Label { Text = p.Nombre, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2C3639") });
                stack.Children.Add(new Label { Text = $"Estado: {p.EstadoNormalizado} • {p.PresupuestoFormateado}", FontSize = 12, TextColor = Color.FromArgb("#7A8485") });

                var tap = new TapGestureRecognizer();
                var projObj = p;
                tap.Tapped += (s, ev) =>
                {
                    UserSession.ActiveProject = projObj;
                    UpdateProjectDisplay();
                    OnCloseSelectProjectSheetClicked(null, EventArgs.Empty);
                    _ = CargarMedicionesGuardadasAsync();
                    _ = ShowToastAsync($"Proyecto activo: {projObj.Nombre}");
                };
                card.GestureRecognizers.Add(tap);
                card.Content = stack;
                ProjectsSelectionList.Children.Add(card);
            }

            SelectProjectSheetModal.IsVisible = true;
            SelectProjectBackdrop.Opacity = 0;
            SelectProjectSheetCard.TranslationY = 400;

            _ = SelectProjectBackdrop.FadeToAsync(1.0, 250);
            await SelectProjectSheetCard.TranslateToAsync(0, 0, 300, Easing.CubicOut);
        }

        private async void OnCloseSelectProjectSheetClicked(object? sender, EventArgs e)
        {
            _ = SelectProjectBackdrop.FadeToAsync(0, 200);
            await SelectProjectSheetCard.TranslateToAsync(0, 400, 250, Easing.CubicIn);
            SelectProjectSheetModal.IsVisible = false;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            try { if (Accelerometer.Default.IsMonitoring) Accelerometer.Default.Stop(); } catch {}
            try { if (Gyroscope.Default.IsMonitoring) Gyroscope.Default.Stop(); } catch {}
            try { if (Compass.Default.IsMonitoring) Compass.Default.Stop(); } catch {}
            try { if (Barometer.Default.IsMonitoring) Barometer.Default.Stop(); } catch {}
            try { Battery.Default.BatteryInfoChanged -= Battery_BatteryInfoChanged; } catch {}
        }

        // --- Acelerómetro ---
        private void OnToggleAccelClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { _ = btn.ScaleToAsync(0.92, 50).ContinueWith(_ => btn.ScaleToAsync(1.0, 50)); }
            if (Accelerometer.Default.IsSupported)
            {
                if (!Accelerometer.Default.IsMonitoring)
                {
                    Accelerometer.Default.ReadingChanged += Accelerometer_ReadingChanged;
                    Accelerometer.Default.Start(SensorSpeed.UI);
                    BtnToggleAccel.Text = "Desactivar";
                    BtnToggleAccel.BackgroundColor = Color.FromArgb("#3F4E4F"); // Lunar Eclipse activo
                }
                else
                {
                    Accelerometer.Default.Stop();
                    Accelerometer.Default.ReadingChanged -= Accelerometer_ReadingChanged;
                    BtnToggleAccel.Text = "Activar";
                    BtnToggleAccel.BackgroundColor = Color.FromArgb("#A27B5B"); // Creme Brulee inactivo
                    AccelLabel.Text = "Detenido";
                }
            }
            else
            {
                AccelLabel.Text = "Acelerómetro no soportado.";
            }
        }

        private double _currentAccelY = 0;
        private double _currentAccelZ = 0;
        
        private double _angleBase = double.NaN;
        private double _angleTop = double.NaN;
        private double _calculatedDistance = 0;

        private void Accelerometer_ReadingChanged(object? sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading;
            _currentAccelY = data.Acceleration.Y;
            _currentAccelZ = data.Acceleration.Z;
            AccelLabel.Text = $"X: {data.Acceleration.X:F2}\nY: {data.Acceleration.Y:F2}\nZ: {data.Acceleration.Z:F2}";
        }

        // --- Giroscopio ---
        private void OnToggleGyroClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { _ = btn.ScaleToAsync(0.92, 50).ContinueWith(_ => btn.ScaleToAsync(1.0, 50)); }
            if (Gyroscope.Default.IsSupported)
            {
                if (!Gyroscope.Default.IsMonitoring)
                {
                    Gyroscope.Default.ReadingChanged += Gyroscope_ReadingChanged;
                    Gyroscope.Default.Start(SensorSpeed.UI);
                    BtnToggleGyro.Text = "Desactivar";
                    BtnToggleGyro.BackgroundColor = Color.FromArgb("#3F4E4F"); // Lunar Eclipse activo
                }
                else
                {
                    Gyroscope.Default.Stop();
                    Gyroscope.Default.ReadingChanged -= Gyroscope_ReadingChanged;
                    BtnToggleGyro.Text = "Activar";
                    BtnToggleGyro.BackgroundColor = Color.FromArgb("#A27B5B"); // Creme Brulee inactivo
                    GyroLabel.Text = "Detenido";
                }
            }
            else
            {
                GyroLabel.Text = "Giroscopio no soportado.";
            }
        }

        private void Gyroscope_ReadingChanged(object? sender, GyroscopeChangedEventArgs e)
        {
            var data = e.Reading;
            GyroLabel.Text = $"X: {data.AngularVelocity.X:F2}\nY: {data.AngularVelocity.Y:F2}\nZ: {data.AngularVelocity.Z:F2}";
        }

        // --- Brújula ---
        private void OnToggleCompassClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { _ = btn.ScaleToAsync(0.92, 50).ContinueWith(_ => btn.ScaleToAsync(1.0, 50)); }
            if (Compass.Default.IsSupported)
            {
                if (!Compass.Default.IsMonitoring)
                {
                    Compass.Default.ReadingChanged += Compass_ReadingChanged;
                    Compass.Default.Start(SensorSpeed.UI);
                    BtnToggleCompass.Text = "Desactivar";
                    BtnToggleCompass.BackgroundColor = Color.FromArgb("#3F4E4F"); // Lunar Eclipse activo
                }
                else
                {
                    Compass.Default.Stop();
                    Compass.Default.ReadingChanged -= Compass_ReadingChanged;
                    BtnToggleCompass.Text = "Activar";
                    BtnToggleCompass.BackgroundColor = Color.FromArgb("#A27B5B"); // Creme Brulee inactivo
                    CompassLabel.Text = "Detenido";
                }
            }
            else
            {
                CompassLabel.Text = "Brújula no soportada.";
            }
        }

        private void Compass_ReadingChanged(object? sender, CompassChangedEventArgs e)
        {
            CompassLabel.Text = $"Rumbo: {e.Reading.HeadingMagneticNorth:F2}º";
        }

        // --- Telémetro Trigonométrico ---
        private async void OnMarkBaseClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { _ = btn.ScaleToAsync(0.92, 50).ContinueWith(_ => btn.ScaleToAsync(1.0, 50)); }
            if (!Accelerometer.Default.IsMonitoring)
            {
                await ShowToastAsync("Activa el Acelerómetro primero en el panel de abajo.");
                return;
            }

            // Calcular ángulo de inclinación respecto a la vertical
            _angleBase = Math.Atan2(_currentAccelZ, -_currentAccelY); 
            TrigResultLabel.Text = $"Base fijada ({_angleBase * 180 / Math.PI:F1}º). Ahora marca el Tope.";
            _ = TrigResultLabel.ScaleToAsync(1.05, 80).ContinueWith(_ => TrigResultLabel.ScaleToAsync(1.0, 80));
        }

        private async void OnMarkTopClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { _ = btn.ScaleToAsync(0.92, 50).ContinueWith(_ => btn.ScaleToAsync(1.0, 50)); }
            if (!Accelerometer.Default.IsMonitoring)
            {
                await ShowToastAsync("Activa el Acelerómetro primero en el panel de abajo.");
                return;
            }

            if (double.IsNaN(_angleBase))
            {
                await ShowToastAsync("Primero debes marcar la base del objeto.");
                return;
            }

            if (!double.TryParse(UserHeightEntry.Text, out double userHeight))
            {
                await ShowToastAsync("Ingresa una altura válida en metros (ej. 1.50).");
                return;
            }

            _angleTop = Math.Atan2(_currentAccelZ, -_currentAccelY);

            // Cálculos Trigonométricos
            _calculatedDistance = userHeight * Math.Abs(Math.Tan(_angleBase));
            double objectHeight = userHeight + (_calculatedDistance * Math.Tan(_angleTop));

            _lastCalculatedObjectHeight = objectHeight;
            BtnSaveTrigMeasure.IsVisible = true;
            _ = BtnSaveTrigMeasure.ScaleToAsync(0.9, 0).ContinueWith(_ => BtnSaveTrigMeasure.ScaleToAsync(1.0, 200, Easing.SpringOut));

            TrigResultLabel.Text = $"Distancia: {_calculatedDistance:F2} m\nAltura del objeto: {objectHeight:F2} m";
            _ = TrigResultLabel.ScaleToAsync(1.05, 80).ContinueWith(_ => TrigResultLabel.ScaleToAsync(1.0, 80));
            
            // Reset for next measurement
            _angleBase = double.NaN;
        }

        // --- Medición por Satélite (GPS) ---
        private Location? _gpsPointA;

        private async void OnGpsPointAClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { _ = btn.ScaleToAsync(0.92, 50).ContinueWith(_ => btn.ScaleToAsync(1.0, 50)); }
            try
            {
                GpsResultLabel.Text = "Buscando señal GPS para Punto A...";
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15));
                _gpsPointA = await Geolocation.Default.GetLocationAsync(request);

                if (_gpsPointA != null)
                {
                    double accuracy = _gpsPointA.Accuracy ?? 999;
                    string accuracyText = accuracy != 999 ? $"(Precisión: ±{accuracy:F0}m)" : "(Precisión desconocida)";
                    
                    if (accuracy > 15)
                    {
                        await ShowToastAsync($"Señal GPS con margen de ±{accuracy:F0}m. Espera unos segundos al aire libre.");
                    }
                    
                    GpsResultLabel.Text = $"Punto A Fijado {accuracyText}. Ve al Punto B.";
                    _ = GpsResultLabel.ScaleToAsync(1.05, 80).ContinueWith(_ => GpsResultLabel.ScaleToAsync(1.0, 80));
                }
            }
            catch (FeatureNotSupportedException) { await ShowToastAsync("El GPS no está soportado en este dispositivo."); }
            catch (PermissionException) { await ShowToastAsync("Falta permiso de ubicación GPS."); }
            catch (Exception ex) { await ShowToastAsync($"Error GPS: {ex.Message}"); }
        }

        private async void OnGpsPointBClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { _ = btn.ScaleToAsync(0.92, 50).ContinueWith(_ => btn.ScaleToAsync(1.0, 50)); }
            if (_gpsPointA == null)
            {
                await ShowToastAsync("Debes fijar el Punto A primero.");
                return;
            }

            try
            {
                GpsResultLabel.Text = "Buscando señal GPS para Punto B...";
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15));
                var pointB = await Geolocation.Default.GetLocationAsync(request);

                if (pointB != null)
                {
                    double accuracyA = _gpsPointA.Accuracy ?? 0;
                    double accuracyB = pointB.Accuracy ?? 0;
                    double totalError = accuracyA + accuracyB;

                    // CalculateDistance returns Kilometers
                    double distanceKm = Location.CalculateDistance(_gpsPointA, pointB, DistanceUnits.Kilometers);
                    double distanceMeters = distanceKm * 1000;
                    
                    _lastGpsDistanceMeters = distanceMeters;
                    _lastGpsPointB = pointB;
                    BtnSaveGpsMeasure.IsVisible = true;
                    _ = BtnSaveGpsMeasure.ScaleToAsync(0.9, 0).ContinueWith(_ => BtnSaveGpsMeasure.ScaleToAsync(1.0, 200, Easing.SpringOut));

                    GpsResultLabel.Text = $"Distancia: {distanceMeters:F1} m (±{totalError:F0} m de margen)";
                    _ = GpsResultLabel.ScaleToAsync(1.05, 80).ContinueWith(_ => GpsResultLabel.ScaleToAsync(1.0, 80));
                }
            }
            catch (Exception ex) { await ShowToastAsync($"Error GPS: {ex.Message}"); }
        }

        // --- Barómetro ---
        private void OnToggleBarometerClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { _ = btn.ScaleToAsync(0.92, 50).ContinueWith(_ => btn.ScaleToAsync(1.0, 50)); }
            if (Barometer.Default.IsSupported)
            {
                if (!Barometer.Default.IsMonitoring)
                {
                    Barometer.Default.ReadingChanged += Barometer_ReadingChanged;
                    Barometer.Default.Start(SensorSpeed.UI);
                    BtnToggleBarometer.Text = "Desactivar";
                    BtnToggleBarometer.BackgroundColor = Color.FromArgb("#3F4E4F"); // Lunar Eclipse activo
                }
                else
                {
                    Barometer.Default.Stop();
                    Barometer.Default.ReadingChanged -= Barometer_ReadingChanged;
                    BtnToggleBarometer.Text = "Activar";
                    BtnToggleBarometer.BackgroundColor = Color.FromArgb("#A27B5B"); // Creme Brulee inactivo
                    BarometerLabel.Text = "Detenido";
                }
            }
            else
            {
                BarometerLabel.Text = "Barómetro no soportado en este dispositivo.";
            }
        }

        private void Barometer_ReadingChanged(object? sender, BarometerChangedEventArgs e)
        {
            var data = e.Reading;
            BarometerLabel.Text = $"Presión: {data.PressureInHectopascals:F2} hPa";
        }

        private void CheckBattery()
        {
            try
            {
                var level = Battery.Default.ChargeLevel; // 0.0 to 1.0
                var state = Battery.Default.State;
                var source = Battery.Default.PowerSource;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (level == -1.0 || level == -1)
                        BatteryLabel.Text = $"Batería no disponible. (Nivel: {level}, Est: {state})";
                    else
                        BatteryLabel.Text = $"Nivel: {level * 100:F0}%\nEstado: {state}\nFuente: {source}";
                });
            }
            catch (FeatureNotSupportedException)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BatteryLabel.Text = "Batería no soportada (FeatureNotSupported).";
                });
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    BatteryLabel.Text = $"Error: {ex.GetType().Name} - {ex.Message}";
                });
            }
        }

        private void OnRefreshBatteryClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { _ = btn.ScaleToAsync(0.92, 50).ContinueWith(_ => btn.ScaleToAsync(1.0, 50)); }
            CheckBattery();
        }

        private void Battery_BatteryInfoChanged(object? sender, BatteryInfoChangedEventArgs e)
        {
            CheckBattery();
        }

        // --- Foto-Medición por Referencia ---
        private Point? _ref1, _ref2, _obj1, _obj2;
        private int _photoMeasureState = 0; // 0=None, 1=WaitRef1, 2=WaitRef2, 3=WaitObj1, 4=WaitObj2, 5=Done
        private double _referenceRealSizeCm = 8.56; // Tarjeta de crédito (largo)
        
        private Microsoft.Maui.Controls.Shapes.Line? _lineRef;
        private Microsoft.Maui.Controls.Shapes.Line? _lineObj;
        private double _panStartX, _panStartY;

        private async void OnTakePhotoForMeasureClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
            if (MediaPicker.Default.IsCaptureSupported)
            {
                try
                {
                    var photo = await MediaPicker.Default.CapturePhotoAsync();
                    if (photo != null)
                    {
                        var stream = await photo.OpenReadAsync();
                        CapturedImage.Source = ImageSource.FromStream(() => stream);
                        ImageContainer.IsVisible = true;
                        
                        // Iniciar máquina de estados
                        _photoMeasureState = 1;
                        PhotoMeasureInstructionLabel.Text = "Paso 1: Toca una esquina de la Tarjeta (Azul)";
                        PhotoMeasureResultLabel.Text = "";
                        DotsLayout.Children.Clear();
                        _lineRef = null;
                        _lineObj = null;
                        BtnResetPhotoMeasure.IsVisible = true;
                        BtnUndoPhotoMeasure.IsVisible = false;
                    }
                }
                catch (Exception ex)
                {
                    await ShowToastAsync($"No se pudo abrir la cámara: {ex.Message}");
                }
            }
        }

        private void OnImageTapped(object? sender, TappedEventArgs e)
        {
            if (_photoMeasureState == 0 || _photoMeasureState == 5) return;

            Point? position = e.GetPosition((View?)sender);
            if (position == null) return;

            Color dotColor = (_photoMeasureState == 1 || _photoMeasureState == 2) ? Color.FromArgb("#3F4E4F") : Color.FromArgb("#A27B5B");

            string dotId = "";
            if (_photoMeasureState == 1) dotId = "ref1";
            else if (_photoMeasureState == 2) dotId = "ref2";
            else if (_photoMeasureState == 3) dotId = "obj1";
            else if (_photoMeasureState == 4) dotId = "obj2";

            var dot = new Microsoft.Maui.Controls.Shapes.Ellipse
            {
                Fill = dotColor,
                Stroke = Colors.White,
                StrokeThickness = 2,
                WidthRequest = 24,
                HeightRequest = 24,
                ClassId = dotId
            };
            
            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += OnDotPanUpdated;
            dot.GestureRecognizers.Add(panGesture);

            AbsoluteLayout.SetLayoutBounds(dot, new Rect(position.Value.X - 12, position.Value.Y - 12, 24, 24));
            DotsLayout.Children.Add(dot);

            if (_photoMeasureState == 1)
            {
                _ref1 = position;
                _photoMeasureState = 2;
                PhotoMeasureInstructionLabel.Text = "Paso 2: Toca la otra esquina de la Tarjeta";
                BtnUndoPhotoMeasure.IsVisible = true;
            }
            else if (_photoMeasureState == 2)
            {
                _ref2 = position;
                
                _lineRef = new Microsoft.Maui.Controls.Shapes.Line
                {
                    X1 = _ref1!.Value.X, Y1 = _ref1.Value.Y,
                    X2 = _ref2.Value.X, Y2 = _ref2.Value.Y,
                    Stroke = Color.FromArgb("#3F4E4F"), StrokeThickness = 3, Opacity = 0.7
                };
                DotsLayout.Children.Insert(0, _lineRef);

                _photoMeasureState = 3;
                PhotoMeasureInstructionLabel.Text = "Paso 3: Toca un extremo del Objeto";
            }
            else if (_photoMeasureState == 3)
            {
                _obj1 = position;
                _photoMeasureState = 4;
                PhotoMeasureInstructionLabel.Text = "Paso 4: Toca el otro extremo del Objeto";
            }
            else if (_photoMeasureState == 4)
            {
                _obj2 = position;
                
                _lineObj = new Microsoft.Maui.Controls.Shapes.Line
                {
                    X1 = _obj1!.Value.X, Y1 = _obj1.Value.Y,
                    X2 = _obj2.Value.X, Y2 = _obj2.Value.Y,
                    Stroke = Color.FromArgb("#A27B5B"), StrokeThickness = 3, Opacity = 0.7
                };
                DotsLayout.Children.Insert(0, _lineObj);

                _photoMeasureState = 5;
                PhotoMeasureInstructionLabel.Text = "Cálculo completado. ¡Puedes arrastrar los puntos para ajustar!";
                BtnUndoPhotoMeasure.IsVisible = false;
                
                CalculatePhotoMeasurement();
            }
        }

        private void OnDotPanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            if (sender is not View dot) return;
            
            if (e.StatusType == GestureStatus.Started)
            {
                var bounds = AbsoluteLayout.GetLayoutBounds(dot);
                _panStartX = bounds.X;
                _panStartY = bounds.Y;
            }
            else if (e.StatusType == GestureStatus.Running)
            {
                double newX = _panStartX + e.TotalX;
                double newY = _panStartY + e.TotalY;
                
                AbsoluteLayout.SetLayoutBounds(dot, new Rect(newX, newY, 24, 24));
                Point newCenter = new Point(newX + 12, newY + 12);
                
                if (dot.ClassId == "ref1") _ref1 = newCenter;
                else if (dot.ClassId == "ref2") _ref2 = newCenter;
                else if (dot.ClassId == "obj1") _obj1 = newCenter;
                else if (dot.ClassId == "obj2") _obj2 = newCenter;
                
                if (_lineRef != null && _ref1.HasValue && _ref2.HasValue)
                {
                    _lineRef.X1 = _ref1.Value.X; _lineRef.Y1 = _ref1.Value.Y;
                    _lineRef.X2 = _ref2.Value.X; _lineRef.Y2 = _ref2.Value.Y;
                }
                
                if (_lineObj != null && _obj1.HasValue && _obj2.HasValue)
                {
                    _lineObj.X1 = _obj1.Value.X; _lineObj.Y1 = _obj1.Value.Y;
                    _lineObj.X2 = _obj2.Value.X; _lineObj.Y2 = _obj2.Value.Y;
                }
                
                if (_photoMeasureState == 5)
                {
                    CalculatePhotoMeasurement();
                }
            }
        }

        private void OnUndoPhotoMeasureClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { _ = btn.ScaleToAsync(0.92, 50).ContinueWith(_ => btn.ScaleToAsync(1.0, 50)); }
            if (_photoMeasureState == 2)
            {
                _ref1 = null;
                _photoMeasureState = 1;
                DotsLayout.Children.RemoveAt(DotsLayout.Children.Count - 1);
                PhotoMeasureInstructionLabel.Text = "Paso 1: Toca una esquina de la Tarjeta";
                BtnUndoPhotoMeasure.IsVisible = false;
            }
            else if (_photoMeasureState == 3)
            {
                _ref2 = null;
                _photoMeasureState = 2;
                DotsLayout.Children.RemoveAt(DotsLayout.Children.Count - 1);
                if (_lineRef != null)
                {
                    DotsLayout.Children.Remove(_lineRef);
                    _lineRef = null;
                }
                PhotoMeasureInstructionLabel.Text = "Paso 2: Toca la otra esquina de la Tarjeta";
            }
            else if (_photoMeasureState == 4)
            {
                _obj1 = null;
                _photoMeasureState = 3;
                DotsLayout.Children.RemoveAt(DotsLayout.Children.Count - 1);
                PhotoMeasureInstructionLabel.Text = "Paso 3: Toca un extremo del Objeto";
            }
        }

        private void CalculatePhotoMeasurement()
        {
            if (_ref1 == null || _ref2 == null || _obj1 == null || _obj2 == null) return;

            double refDx = _ref2.Value.X - _ref1.Value.X;
            double refDy = _ref2.Value.Y - _ref1.Value.Y;
            double refPixels = Math.Sqrt(refDx*refDx + refDy*refDy);

            double objDx = _obj2.Value.X - _obj1.Value.X;
            double objDy = _obj2.Value.Y - _obj1.Value.Y;
            double objPixels = Math.Sqrt(objDx*objDx + objDy*objDy);

            if (refPixels == 0)
            {
                PhotoMeasureResultLabel.Text = "Error: Puntos de tarjeta idénticos.";
                return;
            }

            double ratio = _referenceRealSizeCm / refPixels;
            double objSizeCm = objPixels * ratio;

            _lastPhotoSizeCm = objSizeCm;
            BtnSavePhotoMeasure.IsVisible = true;
            _ = BtnSavePhotoMeasure.ScaleToAsync(0.9, 0).ContinueWith(_ => BtnSavePhotoMeasure.ScaleToAsync(1.0, 200, Easing.SpringOut));

            PhotoMeasureResultLabel.Text = $"Tamaño Objeto: {objSizeCm:F1} cm";
            _ = PhotoMeasureResultLabel.ScaleToAsync(1.05, 80).ContinueWith(_ => PhotoMeasureResultLabel.ScaleToAsync(1.0, 80));
        }

        private void OnResetPhotoMeasureClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { _ = btn.ScaleToAsync(0.92, 50).ContinueWith(_ => btn.ScaleToAsync(1.0, 50)); }
            _photoMeasureState = 1;
            PhotoMeasureInstructionLabel.Text = "Paso 1: Toca una esquina de la Tarjeta";
            PhotoMeasureResultLabel.Text = "";
            DotsLayout.Children.Clear();
            _ref1 = _ref2 = _obj1 = _obj2 = null;
            _lineRef = null;
            _lineObj = null;
            BtnUndoPhotoMeasure.IsVisible = false;
            BtnSavePhotoMeasure.IsVisible = false;
        }

        // ==================== GUARDADO DE MEDICIONES EN EL BACKEND ====================

        private async Task<bool> EnsureActiveProjectAsync()
        {
            if (!UserSession.IsAuthenticated)
            {
                await ShowToastAsync("Debes iniciar sesión para guardar mediciones.");
                return false;
            }

            if (UserSession.ActiveProject == null)
            {
                await ShowToastAsync("Selecciona primero un proyecto destino arriba.");
                OnSelectProjectClicked(null, EventArgs.Empty);
                return false;
            }

            return true;
        }

        private async void OnSaveTrigMeasureClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
            if (!await EnsureActiveProjectAsync()) return;

            var request = new CrearMedicionRequest
            {
                Idproyecto = UserSession.ActiveProject!.Idproyecto,
                Distancia = Math.Round((decimal)_lastCalculatedObjectHeight, 2),
                Puntoinicial = JsonSerializer.Serialize(new { tipo = "Trigonometria_Base", inclinacion = _angleBase }),
                Puntofinal = JsonSerializer.Serialize(new { tipo = "Trigonometria_Tope", distanciaPiso = _calculatedDistance, alturaTotal = _lastCalculatedObjectHeight })
            };

            var (success, message) = await ApiService.GuardarMedicionAsync(request);
            if (success)
            {
                BtnSaveTrigMeasure.IsVisible = false;
                await ShowToastAsync("¡Medición de altura guardada en el backend!");
                await CargarMedicionesGuardadasAsync();
            }
            else
            {
                await ShowToastAsync(message);
            }
        }

        private async void OnSaveGpsMeasureClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
            if (!await EnsureActiveProjectAsync()) return;

            var request = new CrearMedicionRequest
            {
                Idproyecto = UserSession.ActiveProject!.Idproyecto,
                Distancia = Math.Round((decimal)_lastGpsDistanceMeters, 2),
                Puntoinicial = JsonSerializer.Serialize(new { tipo = "GPS_PuntoA" }),
                Puntofinal = JsonSerializer.Serialize(new { tipo = "GPS_PuntoB", distanciaMetros = _lastGpsDistanceMeters })
            };

            var (success, message) = await ApiService.GuardarMedicionAsync(request);
            if (success)
            {
                BtnSaveGpsMeasure.IsVisible = false;
                await ShowToastAsync("¡Distancia GPS guardada en el backend!");
                await CargarMedicionesGuardadasAsync();
            }
            else
            {
                await ShowToastAsync(message);
            }
        }

        private async void OnSavePhotoMeasureClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { await btn.ScaleToAsync(0.95, 60); await btn.ScaleToAsync(1.0, 60); }
            if (!await EnsureActiveProjectAsync()) return;

            var request = new CrearMedicionRequest
            {
                Idproyecto = UserSession.ActiveProject!.Idproyecto,
                Distancia = Math.Round((decimal)(_lastPhotoSizeCm / 100.0), 3),
                Puntoinicial = JsonSerializer.Serialize(new { tipo = "Foto_TarjetaRef", refCm = _referenceRealSizeCm }),
                Puntofinal = JsonSerializer.Serialize(new { tipo = "Foto_ObjetoMedido", tamanoCm = _lastPhotoSizeCm })
            };

            var (success, message) = await ApiService.GuardarMedicionAsync(request);
            if (success)
            {
                BtnSavePhotoMeasure.IsVisible = false;
                await ShowToastAsync("¡Medición fotográfica guardada en el backend!");
                await CargarMedicionesGuardadasAsync();
            }
            else
            {
                await ShowToastAsync(message);
            }
        }

        // ==================== HISTORIAL DE MEDICIONES ====================

        private async void OnRefreshMedicionesClicked(object? sender, EventArgs e)
        {
            if (sender is VisualElement btn) { await btn.ScaleToAsync(0.94, 60); await btn.ScaleToAsync(1.0, 60); }
            await CargarMedicionesGuardadasAsync();
            await ShowToastAsync("Mediciones actualizadas.");
        }

        private async Task CargarMedicionesGuardadasAsync()
        {
            MedicionesGuardadasStack.Children.Clear();

            if (UserSession.ActiveProject == null || !UserSession.IsAuthenticated)
            {
                MedicionesGuardadasStack.Children.Add(new Label
                {
                    Text = "Selecciona un proyecto e inicia sesión para ver mediciones.",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#7A8485"),
                    HorizontalOptions = LayoutOptions.Center,
                    Margin = new Thickness(0, 4)
                });
                return;
            }

            try
            {
                var mediciones = await ApiService.GetMedicionesByProyectoAsync(UserSession.ActiveProject.Idproyecto);

                if (mediciones == null || mediciones.Count == 0)
                {
                    MedicionesGuardadasStack.Children.Add(new Label
                    {
                        Text = "No hay mediciones guardadas en este proyecto.",
                        FontSize = 12,
                        TextColor = Color.FromArgb("#7A8485"),
                        HorizontalOptions = LayoutOptions.Center,
                        Margin = new Thickness(0, 4)
                    });
                    return;
                }

                foreach (var m in mediciones)
                {
                    var card = new Border
                    {
                        BackgroundColor = Color.FromArgb("#F5F3EF"),
                        Stroke = Color.FromArgb("#DCD7C9"),
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
                    stack.Children.Add(new Label { Text = $"Distancia / Altura: {m.DistanciaFormateada}", FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#2C3639") });
                    stack.Children.Add(new Label { Text = $"Registrada: {m.FechaFormateada}", FontSize = 11, TextColor = Color.FromArgb("#7A8485") });

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
                            await ApiService.EliminarMedicionAsync(idMed);
                            await CargarMedicionesGuardadasAsync();
                            await ShowToastAsync("Medición eliminada del proyecto");
                        };
                        delBtn.GestureRecognizers.Add(tap);
                        Grid.SetColumn(delBtn, 1);
                        grid.Children.Add(delBtn);
                    }

                    card.Content = grid;
                    MedicionesGuardadasStack.Children.Add(card);
                }
            }
            catch {}
        }

        // ==================== APPLE TOAST ====================

        private async Task ShowToastAsync(string message)
        {
            AppleToastMessage.Text = message;
            AppleToast.IsVisible = true;
            _ = AppleToast.FadeToAsync(1.0, 200);
            await AppleToast.TranslateToAsync(0, 10, 200, Easing.CubicOut);
            await Task.Delay(2500);
            _ = AppleToast.FadeToAsync(0, 200);
            await AppleToast.TranslateToAsync(0, 0, 200, Easing.CubicIn);
            AppleToast.IsVisible = false;
        }
    }
}
