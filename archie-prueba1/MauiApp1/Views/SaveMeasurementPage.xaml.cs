using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1.Views
{
    public partial class SaveMeasurementPage : ContentPage
    {
        public static event Action? OnMedicionGuardadaLocal;

        private decimal _largoAr = 0;
        private decimal _total = 0;
        private bool _esDeduccion = false;

                public SaveMeasurementPage(decimal largoMedido)
        {
            InitializeComponent();
            _largoAr = largoMedido;
            LargoEntry.Text = _largoAr.ToString("0.00", CultureInfo.InvariantCulture);
            UnidadPicker.SelectedIndex = 0; 
            CalculateTotal();
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            OnMedicionGuardadaLocal?.Invoke();
                await Navigation.PopModalAsync();
        }

                private void OnUnidadChanged(object? sender, EventArgs e)
        {
            string unit = UnidadPicker.SelectedItem?.ToString() ?? "m";
            
            if (unit.Contains("U"))
            {
                LargoEntry.IsEnabled = false;
                AnchoContainer.IsVisible = false;
                AltoContainer.IsVisible = false;
            }
            else if (unit.Contains("m") && unit.Length > 1 && (unit.Contains("2") || unit.Contains("")))
            {
                // Assuming it's m2 (square meters) based on having a char after m. We will differentiate by checking if it contains 3 for volume.
                // Wait, it's safer to check the index if possible. Let's just use SelectedIndex
                if (UnidadPicker.SelectedIndex == 1) // m2
                {
                    LargoEntry.IsEnabled = true;
                    AnchoContainer.IsVisible = true;
                    AltoContainer.IsVisible = false;
                }
                else if (UnidadPicker.SelectedIndex == 2) // m3
                {
                    LargoEntry.IsEnabled = true;
                    AnchoContainer.IsVisible = true;
                    AltoContainer.IsVisible = true;
                }
            }
            else
            {
                if (UnidadPicker.SelectedIndex == 3) // U
                {
                    LargoEntry.IsEnabled = false;
                    AnchoContainer.IsVisible = false;
                    AltoContainer.IsVisible = false;
                }
                else if (UnidadPicker.SelectedIndex == 2) // m3
                {
                    LargoEntry.IsEnabled = true;
                    AnchoContainer.IsVisible = true;
                    AltoContainer.IsVisible = true;
                }
                else if (UnidadPicker.SelectedIndex == 1) // m2
                {
                    LargoEntry.IsEnabled = true;
                    AnchoContainer.IsVisible = true;
                    AltoContainer.IsVisible = false;
                }
                else // m
                {
                    LargoEntry.IsEnabled = true;
                    AnchoContainer.IsVisible = false;
                    AltoContainer.IsVisible = false;
                }
            }
            
            CalculateTotal();
        }

        
        private void OnDeduccionToggled(object? sender, ToggledEventArgs e)
        {
            _esDeduccion = e.Value;
            CalculateTotal();
        }

        private void OnValuesChanged(object? sender, TextChangedEventArgs e)
        {
            CalculateTotal();
        }

                private void CalculateTotal()
        {
            if (VecesEntry == null) return;

            int veces = 1;
            if (!string.IsNullOrWhiteSpace(VecesEntry.Text))
                int.TryParse(VecesEntry.Text, out veces);
            
            decimal largo = 1;
            if (LargoEntry != null && LargoEntry.IsEnabled && !string.IsNullOrWhiteSpace(LargoEntry.Text))
                decimal.TryParse(LargoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out largo);

            decimal ancho = 1;
            if (AnchoContainer != null && AnchoContainer.IsVisible && !string.IsNullOrWhiteSpace(AnchoEntry.Text))
                decimal.TryParse(AnchoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out ancho);
            
            decimal alto = 1;
            if (AltoContainer != null && AltoContainer.IsVisible && !string.IsNullOrWhiteSpace(AltoEntry.Text))
                decimal.TryParse(AltoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out alto);

            _total = veces * largo * ancho * alto;

            if (_esDeduccion)
            {
                _total *= -1;
            }

            TotalLabel.Text = _total.ToString("0.00", CultureInfo.InvariantCulture);
        }

                private async void OnSaveClicked(object? sender, EventArgs e)
        {
            if (UserSession.ActiveProject == null)
            {
                await AlertService.ShowAlertAsync("Aviso", "No hay un proyecto activo.", "OK");
                return;
            }

            int veces = 1;
            int.TryParse(VecesEntry.Text, out veces);
            
            decimal largo = 1;
            if (LargoEntry.IsEnabled && !string.IsNullOrWhiteSpace(LargoEntry.Text))
                decimal.TryParse(LargoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out largo);

            decimal ancho = 1;
            if (AnchoContainer.IsVisible && !string.IsNullOrWhiteSpace(AnchoEntry.Text))
                decimal.TryParse(AnchoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out ancho);
            
            decimal alto = 1;
            if (AltoContainer.IsVisible && !string.IsNullOrWhiteSpace(AltoEntry.Text))
                decimal.TryParse(AltoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out alto);

            var req = new CrearMediciónRequest
            {
                Idproyecto = UserSession.ActiveProject.Idproyecto,
                Distancia = largo, 
                Puntoinicial = "{\"x\":0, \"y\":0, \"z\":0}",
                Puntofinal = "{\"x\":0, \"y\":0, \"z\":0}",
                Fechamedicion = DateTime.UtcNow,
                
                Etapa = EtapaEntry.Text,
                Partida = PartidaEntry.Text,
                Descripcion = DescEntry.Text,
                Veces = veces,
                Largo = largo,
                Ancho = ancho,
                Alto = alto,
                Unidad = UnidadPicker.SelectedItem?.ToString() ?? "m",
                Totalparcial = _total
            };

            var (success, msg) = await ApiService.GuardarMediciónAsync(req);
            if (success)
            {
                await AlertService.ShowAlertAsync("Éxito", $"Cómputo guardado correctamente.", "OK");
                OnMedicionGuardadaLocal?.Invoke();
                await Navigation.PopModalAsync();
            }
            else
            {
                await AlertService.ShowAlertAsync("Error", $"No se guardó: {msg}", "OK");
            }
        }
    }
}

