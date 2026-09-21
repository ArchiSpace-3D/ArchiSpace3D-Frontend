import os

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Views\ProfilePage.xaml"
with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
    content = f.read()

# Fix the padding in MainScroll
content = content.replace('Padding="20,40,20,30"', 'Padding="20,40,20,120"')

# While we're at it, let's also fix the theme icon here to use AppThemeBinding like we did for Dashboard
old_theme_icon = 'Image x:Name="ThemeIconImage" Source="ic_moon.svg"'
new_theme_icon = 'Image x:Name="ThemeIconImage" Source="{AppThemeBinding Light=ic_moon_dark.svg, Dark=ic_sun.svg}"'
content = content.replace(old_theme_icon, new_theme_icon)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
