import os
import re

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Views\SaveMeasurementPage.xaml.cs"
with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
    content = f.read()

# Add the static event declaration
content = content.replace("public partial class SaveMeasurementPage : ContentPage\n    {", "public partial class SaveMeasurementPage : ContentPage\n    {\n        public static event Action? OnMedicionGuardadaLocal;\n")

# Remove MessagingCenter calls and replace with the event
content = content.replace('MessagingCenter.Send<object>(this, "MedicionGuardada");', 'OnMedicionGuardadaLocal?.Invoke();')

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
