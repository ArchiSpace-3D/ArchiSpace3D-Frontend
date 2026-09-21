import os
import re

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Views\SaveMeasurementPage.xaml.cs"
with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
    content = f.read()

new_save = """        private async void OnSaveClicked(object? sender, EventArgs e)
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
                decimal.TryParse(AltoEntry.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out alto);"""

content = re.sub(r'private async void OnSaveClicked\(.*?\)\s*\{.*?decimal alto = 0;\s*decimal.TryParse\(AltoEntry\.Text, NumberStyles\.Any, CultureInfo\.InvariantCulture, out alto\);', new_save, content, flags=re.DOTALL)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
