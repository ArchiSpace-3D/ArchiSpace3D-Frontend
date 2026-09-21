import os
import re

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Controls\FloatingTabBar.xaml"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Remove the broken InputTransparent properties
content = content.replace('InputTransparent="True" CascadeInputTransparent="False"', 'VerticalOptions="End" HorizontalOptions="Center"')
content = content.replace('InputTransparent="False"', '')

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
