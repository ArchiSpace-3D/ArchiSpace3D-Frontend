using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using MauiApp1.Models;
using MauiApp1.Services;

namespace MauiApp1.Views
{
    public partial class SaveMeasurementPage : ContentPage
    {
        private decimal _largoAr = 0;
        private decimal _total = 0;

                public SaveMeasurementPage(decimal largoMedido)
        {
            InitializeComponent();
            _largoAr = largoMedido;
            LargoEntry.Text = _largoAr.ToString("0.00", CultureInfo.InvariantCulture);
            UnidadPicker.SelectedIndex = 0; // Default a 'm'
            CalculateTotal();
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private void OnUnidadChanged(object? sender, EventArgs e)
        {
            string unit = UnidadPicker.SelectedItem?.ToString() ?? "m";
            
            if (unit == "m")
            {
                AnchoContainer.IsVisible = false;
                AltoContainer.IsVisible = false;
            }
            else if (unit == "m²")
            {
                AnchoContainer.IsVisible = true;
                AltoContainer.IsVisible = false;
            }
            else if (unit == "m³")
            {
                AnchoContainer.IsVisible = true;
                AltoContainer.IsVisible = true;
            }
            else if (unit == "U")
            {
                AnchoContainer.IsVisible = false;
                AltoContainer.IsVisible = false;
            }
            
            CalculateTotal();
        }

        private void OnValuesChanged(object? sender, TextChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void CalculateTotal()
        {
            if (VecesEntry == null) return;

            string unit = UnidadPicker.SelectedItem?.ToString() ?? "m";
            
            int veces = 1;
            if (!string.IsNullOrWhiteSpace(VecesEntry.Text))
                int.TryParse(VecesEntry.Text, out veces);
            
            decimal largo = 0;
            if (LargoEntry != null && !string.IsNullOrWhiteSpace(LargoEntry.Text))
                decimal.TryParse(LargoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out largo);

            decimal ancho = 0;
            if (AnchoContainer.IsVisible && !string.IsNullOrWhiteSpace(AnchoEntry.Text))
                decimal.TryParse(AnchoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out ancho);
            
            decimal alto = 0;
            if (AltoContainer.IsVisible && !string.IsNullOrWhiteSpace(AltoEntry.Text))
                decimal.TryParse(AltoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out alto);

            if (unit == "m") _total = veces * largo;
                        else if (unit == "m²") _total = veces * largo * ancho;
            else if (unit == "m³") _total = veces * largo * ancho * alto;
            else if (unit == "U") _total = veces;

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
            
            decimal largo = 0;
            if (LargoEntry != null && !string.IsNullOrWhiteSpace(LargoEntry.Text))
                decimal.TryParse(LargoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out largo);

            decimal ancho = 0;
            decimal.TryParse(AnchoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out ancho);
            decimal alto = 0;
            decimal.TryParse(AltoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out alto);

            var req = new CrearMediciónRequest
            {
                Idproyecto = UserSession.ActiveProject.Idproyecto,
                Distancia = largo, // Retaining original DB logic mapping
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
                await Navigation.PopModalAsync();
            }
            else
            {
                await AlertService.ShowAlertAsync("Error", $"No se guardó: {msg}", "OK");
            }
        }
    }
}


