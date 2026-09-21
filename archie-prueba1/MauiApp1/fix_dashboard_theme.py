import os
import re

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Views\DashboardPage.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Remove the manual source assignments
content = content.replace('ThemeIconDashboard.Source = "ic_moon.svg";', '')
content = content.replace('ThemeIconDashboard.Source = "ic_sun.svg";', '')

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
