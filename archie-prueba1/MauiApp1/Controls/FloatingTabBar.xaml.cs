using MauiApp1.Views;

namespace MauiApp1.Controls;

public partial class FloatingTabBar : ContentView
{
    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(FloatingTabBar), 0,
            propertyChanged: OnSelectedIndexChanged);

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public FloatingTabBar()
    {
        InitializeComponent();
        UpdateTabs(SelectedIndex);
    }

    private static void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is FloatingTabBar tab)
            tab.UpdateTabs((int)newValue);
    }

    private void UpdateTabs(int index)
    {
        var backgrounds = new[] { Bg0, Bg1, Bg2, Bg3, Bg4 };

        for (int i = 0; i < backgrounds.Length; i++)
        {
            // Tab seleccionado: gris claro tipo Instagram
            // Tabs no seleccionados: transparente
            backgrounds[i].BackgroundColor = (i == index)
                ? Color.FromArgb("#E5E5EA")
                : Colors.Transparent;
        }
    }

    private async void OnTabTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is not string s || !int.TryParse(s, out int index)) return;
        if (index == SelectedIndex) return;

        SelectedIndex = index;

        switch (index)
        {
            case 0:
                await Shell.Current.GoToAsync("//DashboardPage");
                break;
            case 1:
                await Shell.Current.GoToAsync("//DesignPage");
                break;
            case 2:
                await Shell.Current.GoToAsync("//DashboardPage");
                break;
            case 3:
                await Navigation.PushModalAsync(new NotificationsPage());
                break;
            case 4:
                await Shell.Current.GoToAsync("//ProfilePage");
                break;
        }
    }
}