import os
import re

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Views\DashboardPage.xaml"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Remove it from the end
content = content.replace('<controls:FloatingTabBar SelectedIndex="0" />', '')

# 2. Insert it right after </ScrollView>
# We need to find </ScrollView>
content = content.replace('</ScrollView>', '</ScrollView>\n\n        <!-- TabBar debe ir DETRÁS de los modales para que no bloquee los botones -->\n        <controls:FloatingTabBar SelectedIndex="0" />')

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
