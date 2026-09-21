import os
import re

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Controls\FloatingTabBar.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Replace the catch block with catch and finally
old_block = """            catch
            {
                // Si la navegación falla, revertimos el estado visual
                UpdateVisualStates(SelectedIndex);
            }"""

new_block = """            catch
            {
                // Fallo silencioso en la navegación
            }
            finally
            {
                // Siempre revertimos la instancia actual a su SelectedIndex real.
                // Así cuando el usuario regrese a esta página (que MAUI mantiene viva en memoria), 
                // el botón correcto seguirá estando iluminado.
                UpdateVisualStates(SelectedIndex);
            }"""

content = content.replace(old_block, new_block)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
