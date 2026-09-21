import os
import re

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Views\DashboardPage.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

old_edit = """            p.Nombre = req.Nombre;
            p.Descripcion = req.Descripcion;
            p.Ubicacion = req.Ubicacion;
            p.Presupuesto = req.Presupuesto;
            
            await CloseEditProjectSheet();"""
            
new_edit = """            p.Nombre = req.Nombre;
            p.Descripcion = req.Descripcion;
            p.Ubicacion = req.Ubicacion;
            p.Presupuesto = req.Presupuesto;
            p.Estado = req.Estado;
            
            await CloseEditProjectSheet();"""

content = content.replace(old_edit, new_edit)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
