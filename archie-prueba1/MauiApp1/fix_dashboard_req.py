import os

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Views\DashboardPage.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

old_req = """        var req = new ActualizarProyectoRequest
        {
            Nombre = EditNombreProyecto.Text ?? p.Nombre,"""

new_req = """        var req = new ActualizarProyectoRequest
        {
            Idproyecto = p.Idproyecto,
            Nombre = EditNombreProyecto.Text ?? p.Nombre,"""

content = content.replace(old_req, new_req)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
