import os
import re

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Controls\FloatingTabBar.xaml"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Add InputTransparent and CascadeInputTransparent to ContentView
content = content.replace('<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"',
                          '<ContentView xmlns="http://schemas.microsoft.com/dotnet/2021/maui"\n             InputTransparent="True" CascadeInputTransparent="False"')

# Add InputTransparent="False" to the Border
content = content.replace('<Border BackgroundColor="#0F172A"', '<Border BackgroundColor="#0F172A"\n            InputTransparent="False"')

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
