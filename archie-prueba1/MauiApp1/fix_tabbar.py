import os
import re

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Controls\FloatingTabBar.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Fix 1: Add null check to UpdateVisualStates
null_check = """    private void UpdateVisualStates(int index)
    {
        if (Bg0 == null || Bg1 == null || Bg2 == null || Bg3 == null) return;"""
content = content.replace("    private void UpdateVisualStates(int index)\n    {", null_check)

# Fix 2: Wrap GoToAsync in a try-catch and revert visual state on failure
goto_block = """            try
            {
                await Shell.Current.GoToAsync(route, false);
            }
            catch
            {
                // Si la navegación falla, revertimos el estado visual
                UpdateVisualStates(SelectedIndex);
            }"""
content = content.replace("            await Shell.Current.GoToAsync(route, false);", goto_block)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
