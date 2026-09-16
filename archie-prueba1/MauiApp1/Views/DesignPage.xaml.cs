using Microsoft.Maui.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using MauiApp1.Services;

namespace MauiApp1.Views;

public partial class DesignPage : ContentPage
{
    private bool is3DMode = false;
    private string currentMode = "freehand"; // freehand, walls, modules

    public DesignPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadLocalArViewer();
    }

    private async void LoadLocalArViewer()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("cad_editor.html");
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();
            
            var htmlSource = new HtmlWebViewSource();
            htmlSource.Html = html;
            ViewerWebView.Source = htmlSource;
        }
        catch (Exception ex)
        {
            var htmlSource = new HtmlWebViewSource();
            htmlSource.Html = $"<html><body style='background:#f4f6f8; margin:0; display:flex; align-items:center; justify-content:center; height:100vh;'><h1 style='color:#001d39; font-family:sans-serif;'>Simulador CAD Activo</h1><p>{ex.Message}</p></body></html>";
            ViewerWebView.Source = htmlSource;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void OnToggle3DClicked(object sender, EventArgs e)
    {
        is3DMode = !is3DMode;
        ModoLabel.Text = is3DMode ? "Modo: 3D Vista" : "Modo: 2D Plano";
        ViewerWebView.Eval($"if(window.switchCameraMode) window.switchCameraMode({is3DMode.ToString().ToLower()});");
    }

        private void OnModoMoverClicked(object sender, EventArgs e)
    {
        currentMode = "pan";
        ContextTitle.Text = "Modo Mover y Editar";
        ContextDesc.Text = "Arrastra el dedo para mover el plano. Toca la medida de un muro guardado para editar su tamaño.";
        UpdateActiveButton(BtnModoMover);
        ViewerWebView.Eval("if(window.switchMode) window.switchMode('pan');");
    }

    private void OnModoLibreClicked(object sender, EventArgs e)
    {
        currentMode = "freehand";
        ContextTitle.Text = "Dibujo Libre Activado";
        ContextDesc.Text = "Dibuja con tu dedo sobre la cuadrícula para generar los muros automáticamente.";
        UpdateActiveButton(BtnModoLibre);
        ViewerWebView.Eval("if(window.switchMode) window.switchMode('freehand');");
    }

    private void OnModoMurosClicked(object sender, EventArgs e)
    {
        currentMode = "walls";
        ContextTitle.Text = "Muros Secuenciales";
        ContextDesc.Text = "Toca para definir el inicio del muro y luego ingresa la medida exacta.";
        UpdateActiveButton(BtnModoMuros);
        ViewerWebView.Eval("if(window.switchMode) window.switchMode('walls');");
    }

    private void OnModoModulosClicked(object sender, EventArgs e)
    {
        currentMode = "modules";
        ContextTitle.Text = "Módulos Prefabricados";
        ContextDesc.Text = "Arrastra bloques (Salón, Pasillo, etc.) y únelos como un rompecabezas.";
        UpdateActiveButton(BtnModoModulos);
        ViewerWebView.Eval("if(window.switchMode) window.switchMode('modules');");
    }

    private void UpdateActiveButton(Button activeBtn)
    {
        BtnModoMover.Opacity = 0.5;
        BtnModoLibre.Opacity = 0.5;
        BtnModoMuros.Opacity = 0.5;
        BtnModoModulos.Opacity = 0.5;
        activeBtn.Opacity = 1.0;
    }

    private void OnUndoClicked(object sender, EventArgs e)
    {
        ViewerWebView.Eval("if(window.undoLastAction) window.undoLastAction();");
    }

                private async void OnSavePlanClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await ViewerWebView.EvaluateJavaScriptAsync("window.getExportData();");
            if (string.IsNullOrEmpty(result) || result == "null") return;
            
            string jsonData = result;
            try 
            {
                // Si MAUI devuelve el string con comillas dobles extras, lo limpiamos
                if (jsonData.StartsWith("\"") && jsonData.EndsWith("\""))
                {
                    jsonData = System.Text.Json.JsonSerializer.Deserialize<string>(result) ?? result;
                }
                // Limpiar posibles secuencias de escape sueltas (por si Android lo manda con escapes \\)
                jsonData = jsonData.Replace("\\\"", "\"").Replace("\\\\", "\\");
                if (jsonData.StartsWith("\"") && jsonData.EndsWith("\""))
                {
                    jsonData = jsonData.Substring(1, jsonData.Length - 2);
                }
            }
            catch { }

            if (UserSession.ActiveProject == null)
            {
                await DisplayAlert("Aviso", "No hay un proyecto activo vinculado para guardar este plano.", "OK");
                return;
            }

            var existente = await ApiService.GetEspacioFisicoByProyectoAsync(UserSession.ActiveProject.Idproyecto);
            if (existente != null)
            {
                existente.Puntosreferencia = jsonData;
                var (success, msg) = await ApiService.ActualizarEspacioFisicoAsync(existente.Idespaciofisico, existente);
                if (success)
                    await DisplayAlert("Plano Actualizado", "El diseño se ha guardado correctamente en tu proyecto.", "OK");
                else
                    await DisplayAlert("Error", "No se pudo actualizar el plano: " + msg, "OK");
            }
            else
            {
                var req = new MauiApp1.Models.CrearEspacioFisicoRequest
                {
                    Idproyecto = UserSession.ActiveProject.Idproyecto,
                    Puntosreferencia = jsonData
                };
                var (success, msg, data) = await ApiService.GuardarEspacioFisicoAsync(req);
                if (success)
                    await DisplayAlert("Plano Guardado", "El diseño se ha creado y guardado en tu proyecto.", "OK");
                else
                    await DisplayAlert("Error", "No se pudo guardar el plano: " + msg, "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Excepción al guardar: " + ex.Message, "OK");
    }
}

}
