using Microsoft.Maui.Devices.Sensors;

namespace MauiApp1
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnEnterARClicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new ARPage());
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            try 
            {
                CheckBattery();
                Battery.Default.BatteryInfoChanged += Battery_BatteryInfoChanged;
            }
            catch (FeatureNotSupportedException)
            {
                // Ignorar error si no hay soporte de batería
            }
            catch (Exception ex) 
            { 
                BatteryLabel.Text = $"Error al iniciar batería: {ex.Message}"; 
            }
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
        private void OnToggleAccelClicked(object sender, EventArgs e)
        {
            if (Accelerometer.Default.IsSupported)
            {
                if (!Accelerometer.Default.IsMonitoring)
                {
                    Accelerometer.Default.ReadingChanged += Accelerometer_ReadingChanged;
                    Accelerometer.Default.Start(SensorSpeed.UI);
                    BtnToggleAccel.Text = "Desactivar";
                    BtnToggleAccel.BackgroundColor = Color.FromArgb("#FF3B30");
                }
                else
                {
                    Accelerometer.Default.Stop();
                    Accelerometer.Default.ReadingChanged -= Accelerometer_ReadingChanged;
                    BtnToggleAccel.Text = "Activar";
                    BtnToggleAccel.BackgroundColor = Color.FromArgb("#007AFF");
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

        private void Accelerometer_ReadingChanged(object sender, AccelerometerChangedEventArgs e)
        {
            var data = e.Reading;
            _currentAccelY = data.Acceleration.Y;
            _currentAccelZ = data.Acceleration.Z;
            AccelLabel.Text = $"X: {data.Acceleration.X:F2}\nY: {data.Acceleration.Y:F2}\nZ: {data.Acceleration.Z:F2}";
        }

        // --- Giroscopio ---
        private void OnToggleGyroClicked(object sender, EventArgs e)
        {
            if (Gyroscope.Default.IsSupported)
            {
                if (!Gyroscope.Default.IsMonitoring)
                {
                    Gyroscope.Default.ReadingChanged += Gyroscope_ReadingChanged;
                    Gyroscope.Default.Start(SensorSpeed.UI);
                    BtnToggleGyro.Text = "Desactivar";
                    BtnToggleGyro.BackgroundColor = Color.FromArgb("#FF3B30");
                }
                else
                {
                    Gyroscope.Default.Stop();
                    Gyroscope.Default.ReadingChanged -= Gyroscope_ReadingChanged;
                    BtnToggleGyro.Text = "Activar";
                    BtnToggleGyro.BackgroundColor = Color.FromArgb("#007AFF");
                    GyroLabel.Text = "Detenido";
                }
            }
            else
            {
                GyroLabel.Text = "Giroscopio no soportado.";
            }
        }

        private void Gyroscope_ReadingChanged(object sender, GyroscopeChangedEventArgs e)
        {
            var data = e.Reading;
            GyroLabel.Text = $"X: {data.AngularVelocity.X:F2}\nY: {data.AngularVelocity.Y:F2}\nZ: {data.AngularVelocity.Z:F2}";
        }

        // --- Brújula ---
        private void OnToggleCompassClicked(object sender, EventArgs e)
        {
            if (Compass.Default.IsSupported)
            {
                if (!Compass.Default.IsMonitoring)
                {
                    Compass.Default.ReadingChanged += Compass_ReadingChanged;
                    Compass.Default.Start(SensorSpeed.UI);
                    BtnToggleCompass.Text = "Desactivar";
                    BtnToggleCompass.BackgroundColor = Color.FromArgb("#FF3B30");
                }
                else
                {
                    Compass.Default.Stop();
                    Compass.Default.ReadingChanged -= Compass_ReadingChanged;
                    BtnToggleCompass.Text = "Activar";
                    BtnToggleCompass.BackgroundColor = Color.FromArgb("#007AFF");
                    CompassLabel.Text = "Detenido";
                }
            }
            else
            {
                CompassLabel.Text = "Brújula no soportada.";
            }
        }

        private void Compass_ReadingChanged(object sender, CompassChangedEventArgs e)
        {
            CompassLabel.Text = $"Rumbo: {e.Reading.HeadingMagneticNorth:F2}º";
        }

        // --- Telémetro Trigonométrico ---
        private void OnMarkBaseClicked(object sender, EventArgs e)
        {
            if (!Accelerometer.Default.IsMonitoring)
            {
                DisplayAlert("Error", "Por favor, activa el Acelerómetro primero en el panel de arriba.", "OK");
                return;
            }

            // Calcular ángulo de inclinación respecto a la vertical
            // Usamos Atan2 con Y y Z. En MAUI, Y es el eje a lo largo del teléfono, Z sale de la pantalla.
            _angleBase = Math.Atan2(_currentAccelZ, -_currentAccelY); 
            TrigResultLabel.Text = $"Base fijada ({_angleBase * 180 / Math.PI:F1}º). Ahora marca el Tope.";
        }

        private void OnMarkTopClicked(object sender, EventArgs e)
        {
            if (!Accelerometer.Default.IsMonitoring)
            {
                DisplayAlert("Error", "Por favor, activa el Acelerómetro primero en el panel de arriba.", "OK");
                return;
            }

            if (double.IsNaN(_angleBase))
            {
                DisplayAlert("Error", "Primero debes marcar la base del objeto.", "OK");
                return;
            }

            if (!double.TryParse(UserHeightEntry.Text, out double userHeight))
            {
                DisplayAlert("Error", "Ingresa una altura válida en metros (ej. 1.50).", "OK");
                return;
            }

            _angleTop = Math.Atan2(_currentAccelZ, -_currentAccelY);

            // Cálculos Trigonométricos
            // d = h * tan(angle_base)
            // obj_height = h + d * tan(angle_top)  (simplificado)
            
            _calculatedDistance = userHeight * Math.Abs(Math.Tan(_angleBase));
            double objectHeight = userHeight + (_calculatedDistance * Math.Tan(_angleTop));

            TrigResultLabel.Text = $"Distancia: {_calculatedDistance:F2} m\nAltura del objeto: {objectHeight:F2} m";
            
            // Reset for next measurement
            _angleBase = double.NaN;
        }

        // --- Medición por Satélite (GPS) ---
        private Location _gpsPointA;

        private async void OnGpsPointAClicked(object sender, EventArgs e)
        {
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
                        await DisplayAlert("Aviso de Calibración", $"La señal GPS actual tiene un margen de error de ±{accuracy:F0} metros. Para distancias cortas, los resultados pueden variar. Intenta salir al aire libre o esperar unos segundos.", "Entendido");
                    }
                    
                    GpsResultLabel.Text = $"Punto A Fijado {accuracyText}. Ve al Punto B.";
                }
            }
            catch (FeatureNotSupportedException) { await DisplayAlert("Error", "El GPS no está soportado.", "OK"); }
            catch (PermissionException) { await DisplayAlert("Error", "Falta permiso de ubicación.", "OK"); }
            catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
        }

        private async void OnGpsPointBClicked(object sender, EventArgs e)
        {
            if (_gpsPointA == null)
            {
                await DisplayAlert("Error", "Debes fijar el Punto A primero.", "OK");
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
                    
                    GpsResultLabel.Text = $"Distancia: {distanceMeters:F1} m (±{totalError:F0} m de margen)";
                    _gpsPointA = null; // reset
                }
            }
            catch (Exception ex) { await DisplayAlert("Error", ex.Message, "OK"); }
        }

        // --- Barómetro ---
        private void OnToggleBarometerClicked(object sender, EventArgs e)
        {
            if (Barometer.Default.IsSupported)
            {
                if (!Barometer.Default.IsMonitoring)
                {
                    Barometer.Default.ReadingChanged += Barometer_ReadingChanged;
                    Barometer.Default.Start(SensorSpeed.UI);
                    BtnToggleBarometer.Text = "Desactivar";
                    BtnToggleBarometer.BackgroundColor = Color.FromArgb("#FF3B30");
                }
                else
                {
                    Barometer.Default.Stop();
                    Barometer.Default.ReadingChanged -= Barometer_ReadingChanged;
                    BtnToggleBarometer.Text = "Activar";
                    BtnToggleBarometer.BackgroundColor = Color.FromArgb("#007AFF");
                    BarometerLabel.Text = "Detenido";
                }
            }
            else
            {
                BarometerLabel.Text = "Barómetro no soportado en este dispositivo.";
            }
        }

        private void Barometer_ReadingChanged(object sender, BarometerChangedEventArgs e)
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

        private void OnRefreshBatteryClicked(object sender, EventArgs e)
        {
            CheckBattery();
        }

        private void Battery_BatteryInfoChanged(object sender, BatteryInfoChangedEventArgs e)
        {
            CheckBattery();
        }

        // --- Foto-Medición por Referencia ---
        private Point? _ref1, _ref2, _obj1, _obj2;
        private int _photoMeasureState = 0; // 0=None, 1=WaitRef1, 2=WaitRef2, 3=WaitObj1, 4=WaitObj2, 5=Done
        private double _referenceRealSizeCm = 8.56; // Tarjeta de crédito (largo)
        
        private Microsoft.Maui.Controls.Shapes.Line _lineRef;
        private Microsoft.Maui.Controls.Shapes.Line _lineObj;
        private double _panStartX, _panStartY;

        private async void OnTakePhotoForMeasureClicked(object sender, EventArgs e)
        {
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
                    await DisplayAlert("Error", $"No se pudo abrir la cámara: {ex.Message}", "OK");
                }
            }
        }

        private void OnImageTapped(object sender, TappedEventArgs e)
        {
            if (_photoMeasureState == 0 || _photoMeasureState == 5) return;

            Point? position = e.GetPosition((View)sender);
            if (position == null) return;

            Color dotColor = (_photoMeasureState == 1 || _photoMeasureState == 2) ? Colors.Blue : Colors.Green;

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
                WidthRequest = 24, // Tamaño ajustado
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
                PhotoMeasureInstructionLabel.Text = "Paso 2: Toca la otra esquina de la Tarjeta (Azul)";
                BtnUndoPhotoMeasure.IsVisible = true;
            }
            else if (_photoMeasureState == 2)
            {
                _ref2 = position;
                
                _lineRef = new Microsoft.Maui.Controls.Shapes.Line
                {
                    X1 = _ref1.Value.X, Y1 = _ref1.Value.Y,
                    X2 = _ref2.Value.X, Y2 = _ref2.Value.Y,
                    Stroke = Colors.Blue, StrokeThickness = 3, Opacity = 0.6
                };
                DotsLayout.Children.Insert(0, _lineRef);

                _photoMeasureState = 3;
                PhotoMeasureInstructionLabel.Text = "Paso 3: Toca un extremo del Objeto (Verde)";
            }
            else if (_photoMeasureState == 3)
            {
                _obj1 = position;
                _photoMeasureState = 4;
                PhotoMeasureInstructionLabel.Text = "Paso 4: Toca el otro extremo del Objeto (Verde)";
            }
            else if (_photoMeasureState == 4)
            {
                _obj2 = position;
                
                _lineObj = new Microsoft.Maui.Controls.Shapes.Line
                {
                    X1 = _obj1.Value.X, Y1 = _obj1.Value.Y,
                    X2 = _obj2.Value.X, Y2 = _obj2.Value.Y,
                    Stroke = Colors.Green, StrokeThickness = 3, Opacity = 0.6
                };
                DotsLayout.Children.Insert(0, _lineObj);

                _photoMeasureState = 5;
                PhotoMeasureInstructionLabel.Text = "Cálculo completado. ¡Puedes arrastrar los puntos para ajustar!";
                BtnUndoPhotoMeasure.IsVisible = false;
                
                CalculatePhotoMeasurement();
            }
        }

        private void OnDotPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            var dot = (View)sender;
            
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

        private void OnUndoPhotoMeasureClicked(object sender, EventArgs e)
        {
            if (_photoMeasureState == 2)
            {
                _ref1 = null;
                _photoMeasureState = 1;
                DotsLayout.Children.RemoveAt(DotsLayout.Children.Count - 1);
                PhotoMeasureInstructionLabel.Text = "Paso 1: Toca una esquina de la Tarjeta (Azul)";
                BtnUndoPhotoMeasure.IsVisible = false;
            }
            else if (_photoMeasureState == 3)
            {
                _ref2 = null;
                _photoMeasureState = 2;
                DotsLayout.Children.RemoveAt(DotsLayout.Children.Count - 1); // punto
                DotsLayout.Children.Remove(_lineRef); // línea azul
                _lineRef = null;
                PhotoMeasureInstructionLabel.Text = "Paso 2: Toca la otra esquina de la Tarjeta (Azul)";
            }
            else if (_photoMeasureState == 4)
            {
                _obj1 = null;
                _photoMeasureState = 3;
                DotsLayout.Children.RemoveAt(DotsLayout.Children.Count - 1);
                PhotoMeasureInstructionLabel.Text = "Paso 3: Toca un extremo del Objeto (Verde)";
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

            PhotoMeasureResultLabel.Text = $"Tamaño Objeto: {objSizeCm:F1} cm";
        }

        private void OnResetPhotoMeasureClicked(object sender, EventArgs e)
        {
            _photoMeasureState = 1;
            PhotoMeasureInstructionLabel.Text = "Paso 1: Toca una esquina de la Tarjeta (Azul)";
            PhotoMeasureResultLabel.Text = "";
            DotsLayout.Children.Clear();
            _ref1 = _ref2 = _obj1 = _obj2 = null;
            _lineRef = null;
            _lineObj = null;
            BtnUndoPhotoMeasure.IsVisible = false;
        }
    }
}
