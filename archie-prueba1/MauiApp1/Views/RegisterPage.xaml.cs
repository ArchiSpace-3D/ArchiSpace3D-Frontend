using Microsoft.Maui.Controls;
using System;
using MauiApp1.Services;
using MauiApp1.Models;
using System.Threading.Tasks;

namespace MauiApp1.Views
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            
            if (width > height) // Landscape
            {
                RootGrid.ColumnDefinitions.Clear();
                RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4.5, GridUnitType.Star) });
                RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5.5, GridUnitType.Star) });

                Grid.SetColumn(BgImage, 1);
                Grid.SetColumnSpan(BgImage, 1);
                
                Grid.SetColumn(FormCard, 0);
                Grid.SetColumnSpan(FormCard, 1);
                FormCard.Margin = new Thickness(0);
                FormCard.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(0) };
            }
            else // Portrait
            {
                RootGrid.ColumnDefinitions.Clear();
                RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                
                Grid.SetColumn(BgImage, 0);
                Grid.SetColumnSpan(BgImage, 1);

                Grid.SetColumn(FormCard, 0);
                Grid.SetColumnSpan(FormCard, 1);
                FormCard.Margin = new Thickness(24, 0, 24, 0);
                FormCard.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(30) };
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            MainLayout.Opacity = 0;
            MainLayout.TranslationY = 30;
            await Task.WhenAll(
                MainLayout.FadeToAsync(1, 600, Easing.CubicOut),
                MainLayout.TranslateToAsync(0, 0, 600, Easing.CubicOut)
            );
        }

        private async void OnBackClicked(object? sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private void OnToggleRegPasswordVisibility(object? sender, EventArgs e)
        {
            RegPasswordEntry.IsPassword = !RegPasswordEntry.IsPassword;
            RegPasswordToggleIcon.Source = RegPasswordEntry.IsPassword ? "ic_eye_off.svg" : "ic_eye_on.svg";
        }

        private async void OnSubmitRegisterClicked(object? sender, EventArgs e)
        {
            await AlertService.ShowAlertAsync("Aviso", "El registro ha sido migrado al panel principal por ahora.", "OK");
            await Navigation.PopModalAsync();
        }
    }
}


