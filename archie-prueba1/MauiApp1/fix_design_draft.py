import os
import re

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Views\DashboardPage.xaml"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Find the block for EditProjectSheetModal
start_tag = '<Border x:Name="EditProjectSheetModal"'
end_tag = '<!-- Join Code Sheet -->'

# We need to carefully slice it. Let's find the position.
start_idx = content.find(start_tag)
# Instead of assuming the next tag, we can search for the end of the Border tag.
# It ends with </Border> after the VerticalStackLayout.
# A safer way is to use regex.
pattern = r'<Border x:Name="EditProjectSheetModal".*?</Border>'
# Wait, there are nested Borders! Regex .*?</Border> will stop at the FIRST nested border!
