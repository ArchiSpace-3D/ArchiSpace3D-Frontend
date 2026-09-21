import os

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Views\SaveMeasurementPage.xaml.cs"
with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
    content = f.read()

# Add _esDeduccion
content = content.replace("private decimal _total = 0;", "private decimal _total = 0;\n        private bool _esDeduccion = false;")

# Add OnDeduccionToggled
deduccion_method = """
        private void OnDeduccionToggled(object? sender, ToggledEventArgs e)
        {
            _esDeduccion = e.Value;
            CalculateTotal();
        }
"""
content = content.replace("private void OnValuesChanged(object? sender, TextChangedEventArgs e)", deduccion_method + "\n        private void OnValuesChanged(object? sender, TextChangedEventArgs e)")

# Fix OnUnidadChanged
new_unidad = """        private void OnUnidadChanged(object? sender, EventArgs e)
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
        }"""
# Let's just replace the whole OnUnidadChanged method body
import re
content = re.sub(r'private void OnUnidadChanged\(.*?\).*?CalculateTotal\(\);\s*\}', new_unidad, content, flags=re.DOTALL)

# Fix CalculateTotal
new_calc = """        private void CalculateTotal()
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
        }"""

content = re.sub(r'private void CalculateTotal\(\).*?TotalLabel\.Text = _total\.ToString\("0\.00", CultureInfo\.InvariantCulture\);\s*\}', new_calc, content, flags=re.DOTALL)

# Fix OnSaveClicked to include MessagingCenter and correct variables
# We need to make sure we don't mess up the CrearMediciónRequest type.
content = content.replace('await Navigation.PopModalAsync();', 'MessagingCenter.Send<object>(this, "MedicionGuardada");\n                await Navigation.PopModalAsync();')

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
