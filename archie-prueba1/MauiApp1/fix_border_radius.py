import os

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Views\DashboardPage.xaml"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Incorrect:
old_tag = '<Border Grid.Column="1" WidthRequest="32" HeightRequest="32" CornerRadius="16" BackgroundColor="{AppThemeBinding Light=#F1F5F9, Dark=#334155}" StrokeThickness="0" VerticalOptions="Center">'
# Correct:
new_tag = """<Border Grid.Column="1" WidthRequest="32" HeightRequest="32" BackgroundColor="{AppThemeBinding Light=#F1F5F9, Dark=#334155}" StrokeThickness="0" VerticalOptions="Center">
                        <Border.StrokeShape><RoundRectangle CornerRadius="16"/></Border.StrokeShape>"""

content = content.replace(old_tag, new_tag)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
