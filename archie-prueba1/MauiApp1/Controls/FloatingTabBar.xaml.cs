using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MauiApp1.Controls;

public partial class FloatingTabBar : ContentView
{
    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(FloatingTabBar), 0, propertyChanged: OnSelectedIndexChanged);

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

        public FloatingTabBar()
    {
        InitializeComponent();
        UpdateVisualStates(SelectedIndex);
        this.Loaded += (s, e) => UpdateVisualStates(SelectedIndex);
    }

    private static void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FloatingTabBar control)
        {
            control.UpdateVisualStates((int)newValue);
        }
    }

    private async void OnTabTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is string param && int.TryParse(param, out int index))
        {
            if (SelectedIndex == index) return;
            
            UpdateVisualStates(index);
            
            string route = index switch
            {
                0 => "//DashboardPage",
                1 => "//DesignPage",
                2 => "//MainPage",
                3 => "//ProfilePage",
                _ => "//DashboardPage"
            };
            
            try
            {
                await Shell.Current.GoToAsync(route, false);
            }
            catch
            {
                // Fallo silencioso en la navegación
            }
            finally
            {
                // Siempre revertimos la instancia actual a su SelectedIndex real.
                // Así cuando el usuario regrese a esta página (que MAUI mantiene viva en memoria), 
                // el botón correcto seguirá estando iluminado.
                UpdateVisualStates(SelectedIndex);
            }
        }
    }

    private void UpdateVisualStates(int index)
    {
        if (Bg0 == null || Bg1 == null || Bg2 == null || Bg3 == null) return;
        var activeBg = Color.FromArgb("#334155");
        var inactiveBg = Colors.Transparent;

        Bg0.BackgroundColor = inactiveBg; Label0.IsVisible = false; Icon0.Opacity = 0.5;
        Bg1.BackgroundColor = inactiveBg; Label1.IsVisible = false; Icon1.Opacity = 0.5;
        Bg2.BackgroundColor = inactiveBg; Label2.IsVisible = false; Icon2.Opacity = 0.5;
        Bg3.BackgroundColor = inactiveBg; Label3.IsVisible = false; Icon3.Opacity = 0.5;

        switch (index)
        {
            case 0:
                Bg0.BackgroundColor = activeBg; Label0.IsVisible = true; Icon0.Opacity = 1.0;
                break;
            case 1:
                Bg1.BackgroundColor = activeBg; Label1.IsVisible = true; Icon1.Opacity = 1.0;
                break;
            case 2:
                Bg2.BackgroundColor = activeBg; Label2.IsVisible = true; Icon2.Opacity = 1.0;
                break;
            case 3:
                Bg3.BackgroundColor = activeBg; Label3.IsVisible = true; Icon3.Opacity = 1.0;
                break;
        }
    }
}
