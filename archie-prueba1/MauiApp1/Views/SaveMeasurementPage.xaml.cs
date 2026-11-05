using System;
using System.Globalization;
using MauiApp1.Models;
using MauiApp1.Services;
using Microsoft.Maui.Controls;

namespace MauiApp1.Views
{
    public partial class SaveMeasurementPage : ContentPage
    {
        public static event Action? OnMedicionGuardadaLocal;

        private readonly decimal _largoAr;
        private decimal _total;
        private bool _esDeduccion;

        public SaveMeasurementPage(decimal largoMedido)
        {
            InitializeComponent();
            _largoAr = largoMedido;
            LargoEntry.Text = _largoAr > 0
                ? _largoAr.ToString("0.00", CultureInfo.InvariantCulture)
                : string.Empty;

            UnidadPicker.SelectedIndex = 0;
            CalculateTotal();
        }

        private async void OnCloseClicked(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private void OnUnidadChanged(object? sender, EventArgs e)
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

        private static decimal ParseDecimalOrDefault(string? input, decimal defaultValue)
        {
            if (string.IsNullOrWhiteSpace(input)) return defaultValue;
            string clean = input.Replace(",", ".").Trim();
            if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal val))
                return val;
            return defaultValue;
        }

        private void CalculateTotal()
        {
            if (VecesEntry == null) return;

            int veces = 1;
            if (!string.IsNullOrWhiteSpace(VecesEntry.Text))
                int.TryParse(VecesEntry.Text, out veces);
            if (veces <= 0) veces = 1;

            decimal largo = 1m;
            if (LargoEntry != null && LargoEntry.IsEnabled)
                largo = ParseDecimalOrDefault(LargoEntry.Text, 0m);

            decimal ancho = 1m;
            if (AnchoContainer != null && AnchoContainer.IsVisible)
                ancho = ParseDecimalOrDefault(AnchoEntry.Text, 1m);

            decimal alto = 1m;
            if (AltoContainer != null && AltoContainer.IsVisible)
                alto = ParseDecimalOrDefault(AltoEntry.Text, 1m);

            _total = veces * largo * ancho * alto;
            if (_esDeduccion) _total *= -1;

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
            if (veces <= 0) veces = 1;

            decimal largo = 1m;
            if (LargoEntry.IsEnabled)
            {
                largo = ParseDecimalOrDefault(LargoEntry.Text, 0m);
                if (largo <= 0m)
                {
                    await AlertService.ShowAlertAsync("Medida requerida", "Ingresa un largo mayor a 0 metros.", "Entendido");
                    return;
                }
            }

            decimal ancho = AnchoContainer.IsVisible ? ParseDecimalOrDefault(AnchoEntry.Text, 1m) : 1m;
            decimal alto = AltoContainer.IsVisible ? ParseDecimalOrDefault(AltoEntry.Text, 1m) : 1m;

            _total = veces * largo * ancho * alto;
            if (_esDeduccion) _total *= -1;

            BtnGuardarComputo.IsEnabled = false;
            try
            {
                var req = new CrearMediciónRequest
                {
                    Idproyecto = UserSession.ActiveProject.Idproyecto,
                    Distancia = largo,
                    Puntoinicial = "{\"x\":0, \"y\":0, \"z\":0}",
                    Puntofinal = "{\"x\":0, \"y\":0, \"z\":0}",
                    Fechamedicion = DateTime.UtcNow,
                    Etapa = string.IsNullOrWhiteSpace(EtapaEntry.Text) ? "General" : EtapaEntry.Text.Trim(),
                    Partida = string.IsNullOrWhiteSpace(PartidaEntry.Text) ? "Medición" : PartidaEntry.Text.Trim(),
                    Descripcion = string.IsNullOrWhiteSpace(DescEntry.Text) ? null : DescEntry.Text.Trim(),
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
                    OnMedicionGuardadaLocal?.Invoke();
                    await Navigation.PopModalAsync();
                }
                else
                {
                    await AlertService.ShowAlertAsync("Error", $"No se pudo guardar: {msg}", "OK");
                }
            }
            finally
            {
                BtnGuardarComputo.IsEnabled = true;
            }
        }
    }
}
