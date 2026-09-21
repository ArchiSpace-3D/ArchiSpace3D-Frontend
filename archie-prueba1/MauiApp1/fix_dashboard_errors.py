import os
import re

file_path = r"C:\Users\angel\OneDrive\Desktop\Archie\Frontend\ArchiSpace3D-Frontend\archie-prueba1\MauiApp1\Views\DashboardPage.xaml.cs"
with open(file_path, "r", encoding="utf-8") as f:
    content = f.read()

# Fix 1: Add else to OnSubmitEditarProyectoClicked
old_edit = """        var res = await ApiService.ActualizarProyectoAsync(p.Idproyecto, req);
        if (res.Success)
        {
            p.Nombre = req.Nombre;
            p.Descripcion = req.Descripcion;
            p.Ubicacion = req.Ubicacion;
            p.Presupuesto = req.Presupuesto;
            p.Estado = req.Estado;
            
            await CloseEditProjectSheet();
            await CargarProyectosAsync();
            ActualizarProyectoActivoUI();
        }
    }"""
new_edit = """        var res = await ApiService.ActualizarProyectoAsync(p.Idproyecto, req);
        if (res.Success)
        {
            p.Nombre = req.Nombre;
            p.Descripcion = req.Descripcion;
            p.Ubicacion = req.Ubicacion;
            p.Presupuesto = req.Presupuesto;
            p.Estado = req.Estado;
            
            await CloseEditProjectSheet();
            await CargarProyectosAsync();
            ActualizarProyectoActivoUI();
        }
        else
        {
            ShowCustomAlert("Error", res.Message, true);
        }
    }"""
content = content.replace(old_edit, new_edit)

# Fix 2: Add else to OnEliminarProyectoFromDetailsClicked
old_delete = """        var res = await ApiService.EliminarProyectoAsync(UserSession.ActiveProject.Idproyecto);
        if (res.Success)
        {
            UserSession.ActiveProject = null;
            ActualizarProyectoActivoUI();
            await CloseDetailsSheet();
            await CargarProyectosAsync();
        }
    }"""
new_delete = """        var res = await ApiService.EliminarProyectoAsync(UserSession.ActiveProject.Idproyecto);
        if (res.Success)
        {
            UserSession.ActiveProject = null;
            ActualizarProyectoActivoUI();
            await CloseDetailsSheet();
            await CargarProyectosAsync();
        }
        else
        {
            ShowCustomAlert("Error al Eliminar", res.Message, true);
        }
    }"""
content = content.replace(old_delete, new_delete)

with open(file_path, "w", encoding="utf-8") as f:
    f.write(content)
