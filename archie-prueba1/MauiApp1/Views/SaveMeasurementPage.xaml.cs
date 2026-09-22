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
            if (_largoAr > 0)
                LargoEntry.Text = _largoAr.ToString("0.00", CultureInfo.InvariantCulture);
            else
                LargoEntry.Text = ""; // Vacío para que el usuario no envíe 0.00 por accidente

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

        private decimal ParseDecimalSafe(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return 1;
            string clean = input.Replace(",", ".");
            if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
                return val;
            return 1;
        }

        private void CalculateTotal()
        {
            if (VecesEntry == null) return;

            int veces = 1;
            if (!string.IsNullOrWhiteSpace(VecesEntry.Text))
                int.TryParse(VecesEntry.Text, out veces);
            if (veces == 0) veces = 1; // Prevenir multiplicar por 0 accidentalmente
            
            decimal largo = 1;
            if (LargoEntry != null && LargoEntry.IsEnabled)
                largo = ParseDecimalSafe(LargoEntry.Text);

            decimal ancho = 1;
            if (AnchoContainer != null && AnchoContainer.IsVisible)
                ancho = ParseDecimalSafe(AnchoEntry.Text);
            
            decimal alto = 1;
            if (AltoContainer != null && AltoContainer.IsVisible)
                alto = ParseDecimalSafe(AltoEntry.Text);

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
            if (!string.IsNullOrWhiteSpace(VecesEntry.Text))
                int.TryParse(VecesEntry.Text, out veces);
            if (veces == 0) veces = 1;
            
            decimal largo = 1;
            if (LargoEntry.IsEnabled)
                largo = ParseDecimalSafe(LargoEntry.Text);

            decimal ancho = 1;
            if (AnchoContainer.IsVisible)
                ancho = ParseDecimalSafe(AnchoEntry.Text);
            
            decimal alto = 1;
            if (AltoContainer.IsVisible)
                alto = ParseDecimalSafe(AltoEntry.Text);

            // Re-calcular total al guardar por seguridad
            _total = veces * largo * ancho * alto;
            if (_esDeduccion) _total *= -1;

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

